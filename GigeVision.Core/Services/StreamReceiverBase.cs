using GigeVision.Core.Enums;
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
        private const int MinimumReceiveBufferBytes = 8 * 1024 * 1024;
        protected Socket socketRxRaw;
        private DateTime lastFirewallPunchKeepAliveSent;
        private const byte GvspLeaderPacketType = 0x01;
        private const byte GvspDataPacketType = 0x03;
        private const byte GvspDataEndPacketType = 0x02;

        /// <summary>
        /// Receives the GigeStream
        /// </summary>
        public StreamReceiverBase()
        {
            GvspInfo = new GvspInfo();
            MissingPacketTolerance = 2;
            ReceiveTimeoutInMilliseconds = 1000;
            FirewallPunchKeepAliveIntervalInSeconds = 30;
        }

        /// <summary>
        /// The socket receive timeout in milliseconds. Set -1 to infinite timeout
        /// </summary>
        public int ReceiveTimeoutInMilliseconds { get; set; }

        /// <summary>
        /// Time interval from a package to another for firewall traversal. Set value <= 0 to disable it
        /// </summary>
        public int FirewallPunchKeepAliveIntervalInSeconds { get; set; }

        /// <summary>
        /// Event for frame ready
        /// </summary>
        public EventHandler<byte[]> FrameReady { get; set; }

        /// <summary>
        /// Fired alongside <see cref="FrameReady"/> and carries per-frame metadata
        /// (hardware timestamp and frame ID) extracted from the GVSP image leader packet.
        /// </summary>
        public EventHandler<GvspFrameInfo> FrameReadyWithInfo { get; set; }

        /// <summary>
        /// GVSP info for image info
        /// </summary>
        public GvspInfo GvspInfo { get; protected set; }

        /// <summary>
        /// The interval for sending to the camera a package to keep the route opened through the firewall
        /// </summary>
        public string CameraIP { get; set; }

        /// <summary>
        /// The camera source traffic port. Required for firewall traversal traffic
        /// </summary>
        public int CameraSourcePort { get; set; }

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
            socketRxRaw?.Dispose();
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
            uint pixelFormat = 0;

            var packetID = (singlePacket[GvspInfo.PacketIDIndex] << 8) | singlePacket[GvspInfo.PacketIDIndex + 1];
            if (packetID == 0)
            {
                GvspInfo.IsImageData = ((singlePacket[10] << 8) | singlePacket[11]) == 1;
                if (GvspInfo.IsImageData)
                {
                    pixelFormat = (uint)((singlePacket[20] << 24) | (singlePacket[21] << 16) | (singlePacket[22] << 8) | singlePacket[23]);
                    GvspInfo.BytesPerPixel = PixelFormatHelper.GetBytesPerPixelRoundedUp(pixelFormat);
                    GvspInfo.Width = (singlePacket[24] << 24) | (singlePacket[25] << 16) | (singlePacket[26] << 8) | (singlePacket[27]);
                    GvspInfo.Height = (singlePacket[28] << 24) | (singlePacket[29] << 16) | (singlePacket[30] << 8) | (singlePacket[31]);
                }
            }

            // Probe a small packet window and keep the largest data-packet length.
            // Startup can land on a short final packet, which would otherwise inflate FinalPacketID
            // and produce false packet-loss reports on every frame.
            int length = 0;
            int maxPacketLength = 0;
            int dataPacketSamples = 0;
            const int packetLengthProbeCount = 64;
            for (int probe = 0; probe < packetLengthProbeCount; probe++)
            {
                length = socketRxRaw.Receive(singlePacket);
                packetID = (singlePacket[GvspInfo.PacketIDIndex] << 8) | singlePacket[GvspInfo.PacketIDIndex + 1];
                if (packetID > 0 && IsDataPacket(singlePacket[4]))
                {
                    dataPacketSamples++;
                    if (length > maxPacketLength)
                    {
                        maxPacketLength = length;
                    }

                    if (dataPacketSamples >= 2 && length == maxPacketLength)
                    {
                        break;
                    }
                }
            }

            if (maxPacketLength > 0)
            {
                GvspInfo.PacketLength = maxPacketLength;
            }

            IsReceiving = length > 10;
            GvspInfo.PayloadSize = GvspInfo.PacketLength - GvspInfo.PayloadOffset;

            if (GvspInfo.Width > 0 && GvspInfo.Height > 0) //Now we can calculate the final packet ID
            {
                var totalBytesExpectedForOneFrame = PixelFormatHelper.GetFrameSize(GvspInfo.Width, GvspInfo.Height, pixelFormat);
                GvspInfo.FinalPacketID = totalBytesExpectedForOneFrame / GvspInfo.PayloadSize;
                if (totalBytesExpectedForOneFrame % GvspInfo.PayloadSize != 0)
                {
                    GvspInfo.FinalPacketID++;
                }
                int recommendedReceiveBuffer = GvspInfo.PacketLength * GvspInfo.FinalPacketID * 4;
                socketRxRaw.ReceiveBufferSize = Math.Max(MinimumReceiveBufferBytes, recommendedReceiveBuffer);
                GvspInfo.RawImageSize = totalBytesExpectedForOneFrame;
            }
        }

        /// <summary>
        /// Basic buffer swap logic
        /// </summary>
        protected virtual void Receiver()
        {
            int packetID = 0, bufferIndex = 0, bufferLength = 0, bufferStart = 0, length = 0, packetRxCount = 0, packetRxCountClone, bufferIndexClone;
            ulong imageID, lastImageID = 0, lastImageIDClone, deltaImageID;
            ulong currentFrameTimestamp = 0;
            byte[] blockID;
            byte[][] buffer = new byte[2][];
            int frameCounter = 0;
            bool skipUntilNextFrameBoundary = true;
            try
            {
                DetectGvspType();
                buffer[0] = new byte[GvspInfo.RawImageSize];
                buffer[1] = new byte[GvspInfo.RawImageSize];
                Span<byte> singlePacket = stackalloc byte[GvspInfo.PacketLength];

                while (IsReceiving)
                {
                    length = socketRxRaw.Receive(singlePacket);
                    if (IsLeaderPacket(singlePacket[4]))
                    {
                        // Extract 64-bit big-endian timestamp from the image leader.
                        // TimeStampIndex is already set correctly for both GVSP v1 and v2.
                        if (length > GvspInfo.TimeStampIndex + 7)
                        {
                            currentFrameTimestamp =
                                ((ulong)singlePacket[GvspInfo.TimeStampIndex] << 56) |
                                ((ulong)singlePacket[GvspInfo.TimeStampIndex + 1] << 48) |
                                ((ulong)singlePacket[GvspInfo.TimeStampIndex + 2] << 40) |
                                ((ulong)singlePacket[GvspInfo.TimeStampIndex + 3] << 32) |
                                ((ulong)singlePacket[GvspInfo.TimeStampIndex + 4] << 24) |
                                ((ulong)singlePacket[GvspInfo.TimeStampIndex + 5] << 16) |
                                ((ulong)singlePacket[GvspInfo.TimeStampIndex + 6] << 8) |
                                 (ulong)singlePacket[GvspInfo.TimeStampIndex + 7];
                        }
                        continue;
                    }
                    if (IsDataPacket(singlePacket[4])) //Packet
                    {
                        if (skipUntilNextFrameBoundary)
                        {
                            continue;
                        }

                        packetRxCount++;
                        packetID = (singlePacket[GvspInfo.PacketIDIndex] << 8) | singlePacket[GvspInfo.PacketIDIndex + 1];
                        bufferStart = (packetID - 1) * GvspInfo.PayloadSize; //This use buffer length of regular packet
                        bufferLength = length - GvspInfo.PayloadOffset;  //This will only change for final packet
                        singlePacket.Slice(GvspInfo.PayloadOffset, bufferLength).CopyTo(buffer[bufferIndex].AsSpan().Slice(bufferStart, bufferLength));
                        continue;
                    }
                    if (IsDataEndPacket(singlePacket[4]))
                    {
                        // Always read the per-frame actual packet count from the DataEnd packet.
                        // For fixed-size uncompressed formats this equals GvspInfo.FinalPacketID; for
                        // variable-length compressed formats (e.g. QOI_BayerRG8) it varies per frame
                        // and must not be compared against the pre-calculated static value, which
                        // assumes uncompressed size and would cause every compressed frame to be
                        // discarded as "missing packets".
                        int dataEndPacketID = (singlePacket[GvspInfo.PacketIDIndex] << 8) | singlePacket[GvspInfo.PacketIDIndex + 1];
                        int thisFrameFinalPacketID = dataEndPacketID - 1;

                        if (GvspInfo.FinalPacketID == 0)
                        {
                            GvspInfo.FinalPacketID = thisFrameFinalPacketID;
                        }

                        blockID = singlePacket.Slice(GvspInfo.BlockIDIndex, GvspInfo.BlockIDLength).ToArray();
                        Array.Reverse(blockID);
                        Array.Resize(ref blockID, 8);
                        imageID = BitConverter.ToUInt64(blockID);

                        if (skipUntilNextFrameBoundary)
                        {
                            lastImageID = imageID;
                            packetRxCount = 0;
                            skipUntilNextFrameBoundary = false;
                            continue;
                        }

                        packetRxCountClone = packetRxCount;
                        lastImageIDClone = lastImageID;
                        bufferIndexClone = bufferIndex;
                        bufferIndex = bufferIndex == 0 ? 1 : 0; //Swaping buffer
                        packetRxCount = 0;
                        lastImageID = imageID;

                        if (DateTime.Now.Subtract(lastFirewallPunchKeepAliveSent).Seconds >= FirewallPunchKeepAliveIntervalInSeconds && FirewallPunchKeepAliveIntervalInSeconds > 0 && CameraSourcePort > 0 && IPAddress.TryParse(CameraIP, out IPAddress cameraAddress))
                        {
                            lastFirewallPunchKeepAliveSent = DateTime.Now;
                            socketRxRaw.SendTo(new byte[8], new IPEndPoint(cameraAddress, CameraSourcePort));
                        }

                        //Checking if we receive all packets
                        int packetCountDelta = packetRxCountClone - thisFrameFinalPacketID;
                        if (Math.Abs(packetCountDelta) <= MissingPacketTolerance)
                        {
                            ++frameCounter;
                            FrameReady?.Invoke(imageID, buffer[bufferIndexClone]);
                            FrameReadyWithInfo?.Invoke(this, new GvspFrameInfo(imageID, currentFrameTimestamp));
                        }
                        else
                        {
                            string frameLossMessage = packetCountDelta < 0
                                ? $"Frame skipped because {Math.Abs(packetCountDelta)} packets were missing."
                                : $"Frame skipped because {packetCountDelta} unexpected extra packets were received.";
                            Updates?.Invoke(UpdateType.FrameLoss, frameLossMessage);
                        }

                        deltaImageID = imageID - lastImageIDClone;
                        //This <10000 is just to skip the overflow value when the counter (2 or 8 bytes) will complete it should not show false missing images
                        if (deltaImageID != 1 && deltaImageID < 10000)
                        {
                            Updates?.Invoke(UpdateType.FrameLoss, $"{imageID - lastImageIDClone - 1} Image missed between {lastImageIDClone}-{imageID}");
                        }
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

        private static bool IsLeaderPacket(byte packetHeaderType)
        {
            return (packetHeaderType & 0x0F) == GvspLeaderPacketType;
        }

        private static bool IsDataPacket(byte packetHeaderType)
        {
            return (packetHeaderType & 0x0F) == GvspDataPacketType;
        }

        private static bool IsDataEndPacket(byte packetHeaderType)
        {
            return (packetHeaderType & 0x0F) == GvspDataEndPacketType;
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
                PortRx = ((IPEndPoint)socketRxRaw.LocalEndPoint).Port;
                socketRxRaw.ReceiveTimeout = ReceiveTimeoutInMilliseconds > 0 ? ReceiveTimeoutInMilliseconds : 0;

                if (CameraSourcePort > 0 && IPAddress.TryParse(CameraIP, out IPAddress cameraAddress))
                {
                    socketRxRaw.SendTo(new byte[8], new IPEndPoint(cameraAddress, CameraSourcePort));
                    lastFirewallPunchKeepAliveSent = DateTime.Now;
                }

                if (IsMulticast)
                {
                    MulticastOption mcastOption = new(IPAddress.Parse(MulticastIP), IPAddress.Parse(RxIP));
                    socketRxRaw.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption);
                }
                // Use a generous default receive buffer so bursty UDP frame delivery does not overflow
                // before the stream metadata is available to calculate a frame-sized buffer.
                socketRxRaw.ReceiveBufferSize = MinimumReceiveBufferBytes;
            }
            catch (Exception ex)
            {
                Updates?.Invoke(UpdateType.ConnectionIssue, ex.Message);
            }
        }
    }
}