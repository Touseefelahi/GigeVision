using Stira.WpfCore;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        private bool isDecodingAsVersion2;

        /// <summary>
        /// Receives the GigeStream
        /// </summary>
        /// <param name="camera"></param>
        public StreamReceiver(Camera camera)
        {
            Camera = camera;
            GvspInfo = new GvspInfo();
        }

        public GvspInfo GvspInfo { get; }

        /// <summary>
        /// If software read the GVSP stream as version 2
        /// </summary>
        public bool IsDecodingAsVersion2
        {
            get => isDecodingAsVersion2;
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
                    MulticastOption mcastOption = new(IPAddress.Parse(Camera.MulticastIP), IPAddress.Any);
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

        private void DecodePacketsRawSocket_workUnderprocess()
        {
            int packetID = 0, bufferIndex = 0, bufferLength = 0, bufferStart = 0, length = 0, packetRxCount = 0;
            byte[][] buffer = new byte[2][];
            buffer[0] = new byte[Camera.rawBytes.Length];
            buffer[1] = new byte[Camera.rawBytes.Length];
            IList<ArraySegment<byte>> buffers = new List<ArraySegment<byte>>();
            try
            {
                DetectGvspType(buffer[0]);
                var header = new byte[GvspInfo.PayloadOffset * 1000];
                var payload = new byte[GvspInfo.PayloadSize];
                buffers.Add(new ArraySegment<byte>(header));
                buffers.Add(new ArraySegment<byte>(payload));
                StateObject state = new();
                state.workSocket = socketRxRaw;
                while (Camera.IsStreaming)
                {
                    var result = socketRxRaw.BeginReceive(header, 0, 8, SocketFlags.None,
                        callback: (ar) =>
                        {
                            if (ar.IsCompleted)
                            {
                                if (header[4] == GvspInfo.DataIdentifier) //Packet
                                {
                                    packetRxCount++;
                                    packetID = (header[GvspInfo.PacketIDIndex] << 8) | header[GvspInfo.PacketIDIndex + 1];
                                    bufferStart = (packetID - 1) * GvspInfo.PayloadSize; //This use buffer length of regular packet
                                    StateObject so = (StateObject)ar.AsyncState;
                                    Socket s = so.workSocket;
                                    int read = s.EndReceive(ar);
                                    //socketRxRaw.BeginReceive(buffer[bufferIndex], bufferStart, GvspInfo.PayloadSize, SocketFlags.None,
                                    //callback: (ar1) =>
                                    //{
                                    //    socketRxRaw.EndReceive(ar1);
                                    //},
                                    //state);
                                    return;
                                }
                                if (header[4] == GvspInfo.DataEndIdentifier)
                                {
                                    if (GvspInfo.FinalPacketID == 0)
                                    {
                                        packetID = (header[GvspInfo.PacketIDIndex] << 8) | header[GvspInfo.PacketIDIndex + 1];
                                        GvspInfo.FinalPacketID = packetID - 1;
                                    }
                                    //Checking if we receive all packets. Here 2 means we are allowing 1 packet miss
                                    if (Math.Abs(packetRxCount - GvspInfo.FinalPacketID) < 2)
                                    {
                                        Camera.FrameReady?.Invoke(null, buffer[bufferIndex]);
                                        bufferIndex = bufferIndex == 0 ? 1 : 0;
                                    }
                                    packetRxCount = 0;
                                    // socketRxRaw.EndReceive(ar);
                                }
                            }
                        },
                        state);
                    //length = await socketRxRaw.ReceiveAsync(buffers, SocketFlags.None).ConfigureAwait(false);

                    //if (header[4] == GvspInfo.DataIdentifier) //Packet
                    //{
                    //    packetRxCount++;
                    //    packetID = (header[GvspInfo.PacketIDIndex] << 8) | header[GvspInfo.PacketIDIndex + 1];
                    //    bufferStart = (packetID - 1) * GvspInfo.PayloadSize; //This use buffer length of regular packet
                    //    bufferLength = length - GvspInfo.PayloadOffset;  //This will only change for final packet
                    //                                                     // payload.AsSpan()[..bufferLength].CopyTo(buffer[bufferIndex].AsSpan().Slice(bufferStart, bufferLength));
                    //    continue;
                    //}
                    //if (header[4] == GvspInfo.DataEndIdentifier)
                    //{
                    //    if (GvspInfo.FinalPacketID == 0)
                    //    {
                    //        packetID = (header[GvspInfo.PacketIDIndex] << 8) | header[GvspInfo.PacketIDIndex + 1];
                    //        GvspInfo.FinalPacketID = packetID - 1;
                    //    }
                    //    //Checking if we receive all packets. Here 2 means we are allowing 1 packet miss
                    //    if (Math.Abs(packetRxCount - GvspInfo.FinalPacketID) < 2)
                    //    {
                    //        Camera.FrameReady?.Invoke(null, buffer[bufferIndex]);
                    //        bufferIndex = bufferIndex == 0 ? 1 : 0;
                    //    }
                    //    packetRxCount = 0;
                    //}
                }
            }
            catch (Exception ex)
            {
                Camera.Updates?.Invoke(null, ex.Message);
                _ = Camera.StopStream();
            }
        }

        private void EndReceive(IAsyncResult ar)
        {
            throw new NotImplementedException();
        }

        private void DecodePacketsRawSocket()
        {
            int packetID = 0, bufferIndex = 0, bufferLength = 0, bufferStart = 0, length = 0, packetRxCount = 0;
            byte[][] buffer = new byte[2][];
            buffer[0] = new byte[Camera.rawBytes.Length];
            buffer[1] = new byte[Camera.rawBytes.Length];
            try
            {
                DetectGvspType(buffer[0]);
                Span<byte> singlePacket = new byte[GvspInfo.PacketLength];
                while (Camera.IsStreaming)
                {
                    length = socketRxRaw.Receive(singlePacket);
                    if (singlePacket[4] == GvspInfo.DataIdentifier) //Packet
                    {
                        packetRxCount++;
                        packetID = (singlePacket[GvspInfo.PacketIDIndex] << 8) | singlePacket[GvspInfo.PacketIDIndex + 1];
                        bufferStart = (packetID - 1) * GvspInfo.PayloadSize; //This use buffer length of regular packet
                        bufferLength = length - GvspInfo.PayloadOffset;  //This will only change for final packet
                        singlePacket.Slice(GvspInfo.PayloadOffset, bufferLength).CopyTo(buffer[bufferIndex].AsSpan().Slice(bufferStart, bufferLength));
                        continue;
                    }
                    if (singlePacket[4] == GvspInfo.DataEndIdentifier)
                    {
                        if (GvspInfo.FinalPacketID == 0)
                        {
                            packetID = (singlePacket[GvspInfo.PacketIDIndex] << 8) | singlePacket[GvspInfo.PacketIDIndex + 1];
                            GvspInfo.FinalPacketID = packetID - 1;
                        }
                        //Checking if we receive all packets. Here 2 means we are allowing 1 packet miss
                        if (Math.Abs(packetRxCount - GvspInfo.FinalPacketID) < 2)
                        {
                            Camera.FrameReady?.Invoke(null, buffer[bufferIndex]);
                            bufferIndex = bufferIndex == 0 ? 1 : 0;
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

        private void DetectGvspType(Span<byte> cameraRawPacket)
        {
            Span<byte> singlePacket = new byte[10000];
            socketRxRaw.Receive(singlePacket);
            GvspInfo.PayloadOffset = 8;
            GvspInfo.PacketIDIndex = 6;
            GvspInfo.DataIdentifier = 0x03;
            GvspInfo.DataEndIdentifier = 0x02;
            IsDecodingAsVersion2 = ((singlePacket[4] & 0xF0) >> 4) == 8;
            if (IsDecodingAsVersion2)
            {
                GvspInfo.PayloadOffset = 20;
                GvspInfo.PacketIDIndex = 18;
                GvspInfo.DataIdentifier = 0x83;
                GvspInfo.DataEndIdentifier = 0x82;
            }
            //Optimizing the array length for receive buffer
            int length = socketRxRaw.Receive(singlePacket);
            int packetID = (singlePacket[GvspInfo.PacketIDIndex] << 8) | singlePacket[GvspInfo.PacketIDIndex + 1];
            if (packetID > 0)
            {
                GvspInfo.PacketLength = length;
            }
            if (packetID == 1)
            {
                singlePacket.CopyTo(cameraRawPacket);
            }
            Camera.IsStreaming = length > 10;
            GvspInfo.PayloadSize = GvspInfo.PacketLength - GvspInfo.PayloadOffset;
            GvspInfo.FinalPacketID = 0;
        }

        public class StateObject
        {
            public const int BUFFER_SIZE = 5510880;
            public Socket workSocket = null;
            public byte[] buffer = new byte[BUFFER_SIZE];
            public StringBuilder sb = new StringBuilder();
        }
    }
}