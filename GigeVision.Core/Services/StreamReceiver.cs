using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Receives the stream
    /// </summary>
    public class StreamReceiver
    {
        private readonly Camera Camera;
        private Socket socketRxRaw;
        private int finalPacketID = 0;

        /// <summary>
        /// Receives the GigeStream
        /// </summary>
        /// <param name="camera"></param>
        public StreamReceiver(Camera camera)
        {
            Camera = camera;
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
            Thread threadDecode = new Thread(DecodePacketsRawSocket)
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
            //Todo: make a rolling buffer here and swap the memory
            int packetID = 0;
            int bufferLength = 0;
            byte[] singlePacketBuf = new byte[10000];
            Span<byte> singlePacket = new Span<byte>(singlePacketBuf);
            Span<byte> cameraRawPacket = new Span<byte>(Camera.rawBytes);
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
                            if (Camera.frameReadyAction != null)
                            {
                                Camera.frameReadyAction?.Invoke(Camera.rawBytes);
                            }
                            else
                            {
                                Camera.FrameReady?.Invoke(null, Camera.rawBytes);
                            }
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