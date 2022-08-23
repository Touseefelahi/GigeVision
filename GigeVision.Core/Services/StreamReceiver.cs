using Stira.WpfCore;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using GigeVision.Core.Enums;
using System.Threading.Tasks;
using System.Buffers;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Microsoft.Toolkit.HighPerformance;

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
                    MulticastOption mcastOption = new(IPAddress.Parse(Camera.MulticastIP), IPAddress.Parse(Camera.RxIP));
                    socketRxRaw.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption);
                }
                socketRxRaw.ReceiveTimeout = 1000;
                socketRxRaw.ReceiveBufferSize = (int)(Camera.Payload * Camera.Height * 5);
            }
            catch (Exception ex)
            {
                Camera.Updates?.Invoke(UpdateType.ConnectionIssue, ex.Message);
            }
        }

        private void DecodePacketsRawSocket_workUnderprocess()
        {
            int packetID = 0, bufferIndex = 0, bufferLength = 0, bufferStart = 0, length = 0, packetRxCount = 1, packetRxCountClone, bufferIndexClone;
            ulong imageID, lastImageID = 0, lastImageIDClone, deltaImageID;
            byte[] blockID;
            byte[][] buffer = new byte[2][];
            buffer[0] = new byte[Camera.rawBytes.Length];
            buffer[1] = new byte[Camera.rawBytes.Length];
            int frameCounter = 0;
            try
            {
                DetectGvspType();
                Span<byte> singlePacket = stackalloc byte[GvspInfo.PacketLength];

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

                        blockID = singlePacket.Slice(GvspInfo.BlockIDIndex, GvspInfo.BlockIDLength).ToArray();
                        Array.Reverse(blockID);
                        Array.Resize(ref blockID, 8);
                        imageID = BitConverter.ToUInt64(blockID);
                        packetRxCountClone = packetRxCount;
                        lastImageIDClone = lastImageID;
                        bufferIndexClone = bufferIndex;
                        bufferIndex = bufferIndex == 0 ? 1 : 0; //Swaping buffer
                        packetRxCount = 0;
                        lastImageID = imageID;

                        Task.Run(() =>
                                    {
                            //Checking if we receive all packets
                            if (Math.Abs(packetRxCountClone - GvspInfo.FinalPacketID) <= Camera.MissingPacketTolerance)
                            {
                                ++frameCounter;
                                Camera.FrameReady?.Invoke(imageID, buffer[bufferIndex]);
                                    }
                            else
                            {
                                Camera.Updates?.Invoke(UpdateType.FrameLoss, $"Image tx skipped because of {packetRxCountClone - GvspInfo.FinalPacketID} packet loss");
                                }
                            }
                        },
                        state);
                    //length = await socketRxRaw.ReceiveAsync(buffers, SocketFlags.None).ConfigureAwait(false);

                            deltaImageID = imageID - lastImageIDClone;
                            //This <10000 is just to skip the overflow value when the counter (2 or 8 bytes) will complete it should not show false missing images
                            if (deltaImageID != 1 && deltaImageID < 10000)
                            {
                                Camera.Updates?.Invoke(UpdateType.FrameLoss, $"{imageID - lastImageIDClone - 1} Image missed between {lastImageIDClone}-{imageID}");
                }
                        });
            }
                }
            }
            catch (Exception ex)
            {
                if (Camera.IsStreaming) // We didn't delibrately stop the stream
                {
                    Camera.Updates?.Invoke(UpdateType.StreamStopped, ex.Message);
                }
                _ = Camera.StopStream();
            }
        }

        private void DecodePacketsRawSocket()
        {
            int packetID = 0, bufferIndex = 0, bufferLength = 0, bufferStart = 0, length = 0, packetRxCount = 1, packetRxCountClone, bufferIndexClone;
            ulong imageID, lastImageID = 0, lastImageIDClone, deltaImageID;
            byte[] blockID;
            int frameCounter = 0;
            List<int> packetIDList = new();
            try
            {
                using MemoryOwner<byte> buffer = MemoryOwner<byte>.Allocate(Camera.rawBytes.Length);
                DetectGvspType();
                Span<byte> singlePacket = stackalloc byte[GvspInfo.PacketLength];

                while (Camera.IsStreaming)
                {
                    length = socketRxRaw.Receive(singlePacket);
                    if (singlePacket[4] == GvspInfo.DataIdentifier) //Packet
                    {
                        packetRxCount++;
                        packetID = (singlePacket[GvspInfo.PacketIDIndex] << 8) | singlePacket[GvspInfo.PacketIDIndex + 1];
                        bufferStart = (packetID - 1) * GvspInfo.PayloadSize; //This use buffer length of regular packet
                        bufferLength = length - GvspInfo.PayloadOffset;  //This will only change for final packet
                        singlePacket.Slice(GvspInfo.PayloadOffset, bufferLength).CopyTo(buffer.Span.Slice(bufferStart, bufferLength));
                        packetIDList.Add(packetID);
                        continue;
                    }
                    if (singlePacket[4] == GvspInfo.DataEndIdentifier)
                    {
                        if (GvspInfo.FinalPacketID == 0)
                        {
                            packetID = (singlePacket[GvspInfo.PacketIDIndex] << 8) | singlePacket[GvspInfo.PacketIDIndex + 1];
                            GvspInfo.FinalPacketID = packetID - 1;
                        }

                        blockID = singlePacket.Slice(GvspInfo.BlockIDIndex, GvspInfo.BlockIDLength).ToArray();
                        Array.Resize(ref blockID, 8);
                        Array.Reverse(blockID);
                        Array.Resize(ref blockID, 8);
                        imageID = BitConverter.ToUInt64(blockID);
                        packetRxCountClone = packetRxCount;
                        lastImageIDClone = lastImageID;
                        bufferIndexClone = bufferIndex;
                        bufferIndex = bufferIndex == 0 ? 1 : 0; //Swaping buffer
                        packetRxCount = 0;
                        lastImageID = imageID;

                        Task.Run(() => //Send the image ready signal parallel, without breaking the reception
                        {
                            //Checking if we receive all packets
                            if (Math.Abs(packetRxCountClone - GvspInfo.FinalPacketID) <= Camera.MissingPacketTolerance)
                            {
                                ++frameCounter;
                                Camera.FrameReady?.Invoke(imageID, buffer.DangerousGetArray().Array);
                            }
                            else
                            {
                                Camera.Updates?.Invoke(UpdateType.FrameLoss, $"Image tx skipped because of {packetRxCountClone - GvspInfo.FinalPacketID} packet loss");
                            }

                            deltaImageID = imageID - lastImageIDClone;
                            //This <10000 is just to skip the overflow value when the counter (2 or 8 bytes) will complete it should not show false missing images
                            if (deltaImageID != 1 && deltaImageID < 10000)
                            {
                                Camera.Updates?.Invoke(UpdateType.FrameLoss, $"{imageID - lastImageIDClone - 1} Image missed between {lastImageIDClone}-{imageID}");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (Camera.IsStreaming) // We didn't delibrately stop the stream
                {
                    Camera.Updates?.Invoke(UpdateType.StreamStopped, ex.Message);
                }
                _ = Camera.StopStream();
            }
        }

        private async void DecodePacketsRawSockett()
        {
            int packetID = 0, bufferIndex = 0, bufferLength = 0, bufferStart = 0, length = 0, packetRxCount = 1, packetRxCountClone, bufferIndexClone, frameCounter = 0;
            ulong imageID, lastImageID = 0, lastImageIDClone, deltaImageID;
            byte[] blockID;
            byte[][] buffer = new byte[2][];
            buffer[0] = new byte[Camera.rawBytes.Length];
            buffer[1] = new byte[Camera.rawBytes.Length];
            try
            {
                DetectGvspType();
                var singlePacketBuffer = GC.AllocateArray<byte>(GvspInfo.PacketLength, true);
                var singlePacket = singlePacketBuffer.AsMemory();
                List<int> listOfPacketLength = new();
                while (Camera.IsStreaming)
                {
                    var rxResult = await socketRxRaw.ReceiveFromAsync(singlePacket).ConfigureAwait(false);
                    if (rxResult is SocketReceiveFromResult recvResult)
                    {
                        length = recvResult.ReceivedBytes;
                        if (singlePacket.Span[4] == GvspInfo.DataIdentifier) //Packet
                        {
                            packetRxCount++;
                            packetID = (singlePacket.Span[GvspInfo.PacketIDIndex] << 8) | singlePacket.Span[GvspInfo.PacketIDIndex + 1];
                            bufferStart = (packetID - 1) * GvspInfo.PayloadSize; //This use buffer length of regular packet
                            bufferLength = length - GvspInfo.PayloadOffset;  //This will only change for final packet
                            singlePacket.Span.Slice(GvspInfo.PayloadOffset, bufferLength).CopyTo(buffer[bufferIndex].AsSpan().Slice(bufferStart, bufferLength));
                            continue;
                        }
                        if (singlePacket.Span[4] == GvspInfo.DataEndIdentifier)
                        {
                            if (GvspInfo.FinalPacketID == 0)
                            {
                                packetID = (singlePacket.Span[GvspInfo.PacketIDIndex] << 8) | singlePacket.Span[GvspInfo.PacketIDIndex + 1];
                                GvspInfo.FinalPacketID = packetID - 1;
        }


        private void DetectGvspType()
        {
            Span<byte> singlePacket = new byte[10000];
            socketRxRaw.Receive(singlePacket);
            IsDecodingAsVersion2 = ((singlePacket[4] & 0xF0) >> 4) == 8;

            GvspInfo.BlockIDIndex = IsDecodingAsVersion2 ? 8 : 2;
            GvspInfo.BlockIDLength = IsDecodingAsVersion2 ? 8 : 2;
            GvspInfo.PacketIDIndex = IsDecodingAsVersion2 ? 18 : 6;
            GvspInfo.PayloadOffset = IsDecodingAsVersion2 ? 20 : 8;
            GvspInfo.TimeStampIndex = IsDecodingAsVersion2 ? 24 : 12;
            GvspInfo.DataIdentifier = IsDecodingAsVersion2 ? 0x83 : 0x03;
            GvspInfo.DataEndIdentifier = IsDecodingAsVersion2 ? 0x82 : 0x02;

            //Optimizing the array length for receive buffer
            int length = socketRxRaw.Receive(singlePacket);
            int packetID = (singlePacket[GvspInfo.PacketIDIndex] << 8) | singlePacket[GvspInfo.PacketIDIndex + 1];
            if (packetID > 0)
            {
                GvspInfo.PacketLength = length;
            }
            Camera.IsStreaming = length > 10;
            GvspInfo.PayloadSize = GvspInfo.PacketLength - GvspInfo.PayloadOffset;
            GvspInfo.FinalPacketID = 0;
        }
    }
}