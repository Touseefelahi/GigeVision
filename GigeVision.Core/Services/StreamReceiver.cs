using Stira.WpfCore;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Receives the stream
    /// </summary>
    public class StreamReceiver : BaseNotifyPropertyChanged
    {
        private readonly Camera Camera;
        private Socket socketRxRaw;
        private int finalPacketID = 0;
        private bool isDecodingAsVersion2;

        /// <summary>
        /// Receives the GigeStream
        /// </summary>
        /// <param name="camera"></param>
        public StreamReceiver(Camera camera)
        {
            Camera = camera;
        }

        /// <summary>
        /// If software read the GVSP stream as version 2
        /// </summary>
        public bool IsDecodingAsVersion2
        {
            get { return isDecodingAsVersion2; }
            set
            {
                if (isDecodingAsVersion2 != value)
                {
                    isDecodingAsVersion2 = value;
                    OnPropertyChanged(nameof(IsDecodingAsVersion2));
                }
            }
        }

        /// <summary>
        /// Resets the final packet ID
        /// </summary>
        public void ResetPacketSize()
        {
            finalPacketID = 0;
        }

        /// <summary>
        /// Start Rx thread using .Net
        /// </summary>
        public void StartRxThread()
        {
            Thread threadDecode = new(DecodePacketsRawSocket)
            {
                Priority = ThreadPriority.Highest,
                Name = "Decode Packets Thread",
                IsBackground = true
            };
            SetupSocketRxRaw();
            threadDecode.Start();
        }

        private void SetupSocketRxRaw()
        {
            try
            {
                if (socketRxRaw != null)
                {
                    socketRxRaw.Close();
                    socketRxRaw.Dispose();
                }
                socketRxRaw = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socketRxRaw.Bind(new IPEndPoint(IPAddress.Any, Camera.PortRx));
                if (Camera.IsMulticast)
                {
                    MulticastOption mcastOption = new MulticastOption(IPAddress.Parse(Camera.MulticastIP), IPAddress.Any);
                    socketRxRaw.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption);
                }
                socketRxRaw.ReceiveTimeout = 1000;
                socketRxRaw.ReceiveBufferSize = (int)(Camera.Payload * Camera.Height * 5);
            }
            catch (Exception ex)
            {
                Camera.Updates?.Invoke(null, ex.Message);
            }
        }

        private void DecodePacketsRawSocket()
        {
            int packetID = 0, bufferLength = 0, bufferStart = 0;
            Span<byte> singlePacket = new byte[10000];
            Span<byte> cameraRawPacket = new(Camera.rawBytes);
            int packetRxCount = 0; //This is for full packet check
            finalPacketID = 0;
            try
            {
                int length = socketRxRaw.Receive(singlePacket);
                Camera.IsStreaming = length > 10;
                int payloadOffset = 8;
                int packetIDIndex = 6;
                int dataIdentifier = 0x03;
                int dataEndIdentifier = 0x02;
                IsDecodingAsVersion2 = ((singlePacket[4] & 0xF0) >> 4) == 8;
                if (IsDecodingAsVersion2)
                {
                    payloadOffset = 20;
                    packetIDIndex = 18;
                    dataIdentifier = 0x83;
                    dataEndIdentifier = 0x82;
                }
                while (Camera.IsStreaming)
                {
                    length = socketRxRaw.Receive(singlePacket);
                    if (singlePacket[4] == dataIdentifier) //Packet
                    {
                        packetRxCount++;
                        packetID = (singlePacket[packetIDIndex] << 8) | singlePacket[packetIDIndex + 1];
                        if (packetID < finalPacketID) //Check for final packet because final packet length maybe lesser than the regular packets
                        {
                            bufferLength = length - payloadOffset;
                        }
                        bufferStart = (packetID - 1) * bufferLength; //This use buffer length of regular packet
                        bufferLength = length - payloadOffset;  //This will only change for final packet
                        Span<byte> slicedRowInImage = cameraRawPacket.Slice(bufferStart, bufferLength);
                        singlePacket.Slice(payloadOffset, bufferLength).CopyTo(slicedRowInImage);
                        continue;
                    }
                    if (singlePacket[4] == dataEndIdentifier)
                    {
                        if (finalPacketID == 0)
                        {
                            finalPacketID = packetID - 1;
                        }
                        //Checking if we receive all packets. Here 2 means we are allowing 1 packet miss
                        if (Math.Abs(packetRxCount - finalPacketID) < 2)
                        {
                            // Camera.frameReadyAction?.Invoke(Camera.rawBytes);
                            Camera.FrameReady?.Invoke(null, Camera.rawBytes);
                        }
                        packetRxCount = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Camera.Updates?.Invoke(null, ex.Message);
                _ = Camera.StopStream();
            }
        }

        /// <summary>
        /// This is old method for decoding GVSP stream of version 1.2 only
        /// </summary>
        private void DecodePacketsRawSocket_12()
        {
            //Todo: make a rolling buffer here and swap the memory
            int packetID = 0;
            int bufferLength = 0;
            byte[] singlePacketBuf = new byte[10000];
            Span<byte> singlePacket = new(singlePacketBuf);
            Span<byte> cameraRawPacket = new(Camera.rawBytes);
            int packetRxCount = 0;//This is for full packet check
            try
            {
                int length = socketRxRaw.Receive(singlePacket);
                while (Camera.IsStreaming)
                {
                    length = socketRxRaw.Receive(singlePacket);
                    if (singlePacket[4] == 0x03) //Packet
                    {
                        packetRxCount++;
                        packetID = (singlePacket[6] << 8) | singlePacket[7];

                        if (packetID < finalPacketID) //Check for final packet because final packet length maybe lesser than the regular packets
                        {
                            bufferLength = length - 8;
                            Span<byte> slicedRowInImage = cameraRawPacket.Slice((packetID - 1) * bufferLength, bufferLength);
                            singlePacket.Slice(8, bufferLength).CopyTo(slicedRowInImage);
                        }
                        else
                        {
                            Span<byte> slicedRowInImage = cameraRawPacket.Slice((packetID - 1) * bufferLength, length - 8);
                            singlePacket.Slice(8, length - 8).CopyTo(slicedRowInImage);
                        }
                    }
                    else if (singlePacket[4] == 0x02)
                    {
                        if (finalPacketID == 0)
                        {
                            finalPacketID = packetID - 1;
                        }
                        //Checking if we receive all packets. Here 2 means we are allowing 1 packet miss
                        if (Math.Abs(packetRxCount - finalPacketID) < 2)
                        {
                            Camera.FrameReady?.Invoke(null, Camera.rawBytes);
                        }
                        packetRxCount = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Camera.Updates?.Invoke(null, ex.Message);
                _ = Camera.StopStream();
            }
        }
    }
}