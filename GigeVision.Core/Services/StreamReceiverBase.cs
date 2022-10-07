using GigeVision.Core.Enums;
using Stira.WpfCore;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GigeVision.Core.Models;
using GigeVision.Core.Interfaces;

namespace GigeVision.Core.Services
{
    /// <summary>
    /// Receives the stream
    /// </summary>
    public abstract class StreamReceiverBase : BaseNotifyPropertyChanged, IStreamReceiver
    {
        protected Socket socketRxRaw;

        /// <summary>
        /// Receives the GigeStream
        /// </summary>
        public StreamReceiverBase()
        {
            GvspInfo = new GvspInfo();
            MissingPacketTolerance = 2;
        }

        /// <summary>
        /// Event for frame ready
        /// </summary>
        public EventHandler<byte[]> FrameReady { get; set; }

        /// <summary>
        /// GVSP info for image info
        /// </summary>
        public GvspInfo GvspInfo { get; protected set; }

        /// <summary>
        /// Is multicast enabled
        /// </summary>
        public bool IsMulticast { get; set; }

        /// <summary>
        /// Is listening to receive the stream
        /// </summary>
        public bool IsReceiving { get; set; }

        /// <summary>
        /// Missing packet tolerance, if we lost more than this many packets then the frame will be skipped
        /// </summary>
        public int MissingPacketTolerance { get; set; } = 2;

        /// <summary>
        /// Multicast IP, only used if Multicasting is enabled by setting <see cref="IsMulticast"/> as true
        /// </summary>
        public string MulticastIP { get; set; }

        /// <summary>
        /// Receiver port
        /// </summary>
        public int PortRx { get; set; }

        /// <summary>
        /// RX IP, required for multicast group
        /// </summary>
        public string RxIP { get; set; }

        /// <summary>
        /// Event for general updates
        /// </summary>
        public EventHandler<string> Updates { get; set; }

        /// <summary>
        /// Start Rx thread using
        /// </summary>
        public void StartRxThread()
        {
            Thread threadDecode = new(Receiver)
            {
                Priority = ThreadPriority.Highest,
                Name = "Decode Packets Thread",
                IsBackground = true
            };
            SetupSocketRxRaw();
            IsReceiving = true;
            threadDecode.Start();
        }

        /// <summary>
        /// Stop reception thread
        /// </summary>
        public void StopReception()
        {
            IsReceiving = false;
            socketRxRaw?.Close();
            socketRxRaw.Dispose();
        }

        /// <summary>
        /// GVSP leader parser- required only one time
        /// </summary>
        protected void DetectGvspType()
        {
            Span<byte> singlePacket = new byte[9000];
            socketRxRaw.Receive(singlePacket);
            GvspInfo.IsDecodingAsVersion2 = ((singlePacket[4] & 0xF0) >> 4) == 8;
            GvspInfo.SetDecodingTypeParameter();

            var packetID = (singlePacket[GvspInfo.PacketIDIndex] << 8) | singlePacket[GvspInfo.PacketIDIndex + 1];
            if (packetID == 0)
            {
                GvspInfo.IsImageData = ((singlePacket[10] << 8) | singlePacket[11]) == 1;
                if (GvspInfo.IsImageData)
                {
                    GvspInfo.BytesPerPixel = (int)Math.Ceiling((double)(singlePacket[21] / 8));
                    GvspInfo.Width = (singlePacket[24] << 24) | (singlePacket[25] << 16) | (singlePacket[26] << 8) | (singlePacket[27]);
                    GvspInfo.Height = (singlePacket[28] << 24) | (singlePacket[29] << 16) | (singlePacket[30] << 8) | (singlePacket[31]);
                }
            }

            //Optimizing the array length for receive buffer
            int length = socketRxRaw.Receive(singlePacket);
            packetID = (singlePacket[GvspInfo.PacketIDIndex] << 8) | singlePacket[GvspInfo.PacketIDIndex + 1];
            if (packetID > 0)
            {
                GvspInfo.PacketLength = length;
            }
            IsReceiving = length > 10;
            GvspInfo.PayloadSize = GvspInfo.PacketLength - GvspInfo.PayloadOffset;

            if (GvspInfo.Width > 0 && GvspInfo.Height > 0) //Now we can calculate the final packet ID
            {
                var totalBytesExpectedForOneFrame = GvspInfo.Width * GvspInfo.Height;
                GvspInfo.FinalPacketID = totalBytesExpectedForOneFrame / GvspInfo.PayloadSize;
                if (totalBytesExpectedForOneFrame % GvspInfo.PayloadSize != 0)
                {
                    GvspInfo.FinalPacketID++;
                }
                socketRxRaw.ReceiveBufferSize = (GvspInfo.PacketLength * GvspInfo.FinalPacketID); //Single frame with GVSP header
                GvspInfo.RawImageSize = GvspInfo.Width * GvspInfo.Height * GvspInfo.BytesPerPixel;
            }
        }

        /// <summary>
        /// Basic buffer swap logic
        /// </summary>
        protected virtual void Receiver()
        {
            int packetID = 0, bufferIndex = 0, bufferLength = 0, bufferStart = 0, length = 0, packetRxCount = 1, packetRxCountClone, bufferIndexClone;
            ulong imageID, lastImageID = 0, lastImageIDClone, deltaImageID;
            byte[] blockID;
            byte[][] buffer = new byte[2][];
            int frameCounter = 0;
            try
            {
                DetectGvspType();
                buffer[0] = new byte[GvspInfo.RawImageSize];
                buffer[1] = new byte[GvspInfo.RawImageSize];
                Span<byte> singlePacket = stackalloc byte[GvspInfo.PacketLength];

                while (IsReceiving)
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
                            if (Math.Abs(packetRxCountClone - GvspInfo.FinalPacketID) <= MissingPacketTolerance)
                            {
                                ++frameCounter;
                                FrameReady?.Invoke(imageID, buffer[bufferIndex]);
                            }
                            else
                            {
                                Updates?.Invoke(UpdateType.FrameLoss, $"Image tx skipped because of {packetRxCountClone - GvspInfo.FinalPacketID} packet loss");
                            }

                            deltaImageID = imageID - lastImageIDClone;
                            //This <10000 is just to skip the overflow value when the counter (2 or 8 bytes) will complete it should not show false missing images
                            if (deltaImageID != 1 && deltaImageID < 10000)
                            {
                                Updates?.Invoke(UpdateType.FrameLoss, $"{imageID - lastImageIDClone - 1} Image missed between {lastImageIDClone}-{imageID}");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (IsReceiving) // We didn't delibrately stop the stream
                {
                    Updates?.Invoke(UpdateType.StreamStopped, ex.Message);
                }
                IsReceiving = false;
            }
        }

        /// <summary>
        /// Sets up socket parameters
        /// </summary>
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
                socketRxRaw.Bind(new IPEndPoint(IPAddress.Any, PortRx));
                if (IsMulticast)
                {
                    MulticastOption mcastOption = new(IPAddress.Parse(MulticastIP), IPAddress.Parse(RxIP));
                    socketRxRaw.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption);
                }
                socketRxRaw.ReceiveTimeout = 1000;
                //One full hd image with GVSP2.0 Header as default, it will be updated for image type
                socketRxRaw.ReceiveBufferSize = (int)(1920 * 1100);
            }
            catch (Exception ex)
            {
                Updates?.Invoke(UpdateType.ConnectionIssue, ex.Message);
            }
        }
    }
}