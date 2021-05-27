using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Receives the stream
    /// </summary>
    public class StreamReceiver
    {
        private readonly Camera Camera;

        private UdpClient socketRx;
        private IPEndPoint endPoint;
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
        /// It starts Thread using C++ library
        /// </summary>
        public void StartRxCppThread()
        {
            Thread threadDecode = new Thread(RxCpp)
            {
                Priority = ThreadPriority.Highest,
                Name = "Decode C++ Packets Thread",
                IsBackground = true
            };
            threadDecode.Start();
        }

        /// <summary>
        /// Start Rx thread using .Net
        /// </summary>
        public void StartRxThread()
        {
            Action action = null;
            switch (Camera.Gvcp.RegistersDictionary["XmlVersion"])
            {
                case "http://www.genicam.org/GenApi/Version_1_1":
                    action = DecodePacketsRawSocketVersion1;
                    break;

                case "http://www.genicam.org/GenApi/Version_1_0":
                    action = DecodePacketsRawSocketVersion0;
                    break;

                default:
                    break;
            }

            Thread threadDecode = new Thread(action.Invoke)
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
                    var mcastOption = new MulticastOption(IPAddress.Parse(Camera.MulticastIP), IPAddress.Any);
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

        private void DecodePacketsRawSocketVersion1()
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
                    if (singlePacket[4] == 0x83) //Packet
                    {
                        packetRxCount++;
                        packetID = (singlePacket[18] << 8) | singlePacket[19];

                        if (packetID < finalPacketID) //Check for final packet because final packet length maybe lesser than the regular packets
                        {
                            bufferLength = length - 20;
                            Span<byte> slicedRowInImage = cameraRawPacket.Slice((packetID - 1) * bufferLength, bufferLength);
                            singlePacket.Slice(20, bufferLength).CopyTo(slicedRowInImage);
                        }
                        else
                        {
                            Span<byte> slicedRowInImage = cameraRawPacket.Slice((packetID - 1) * bufferLength, length - 20);
                            singlePacket.Slice(20, length - 20).CopyTo(slicedRowInImage);
                        }
                    }
                    else if (singlePacket[4] == 0x82)
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

        private void DecodePacketsRawSocketVersion0()
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

        #region OldCode

        private IntPtr intPtr;

        private void SetupSocketRx()
        {
            try
            {
                socketRx?.Client.Close();
                socketRx?.Close();
            }
            catch (Exception) { }
            try
            {
                socketRx = new UdpClient(Camera.PortRx);
                endPoint = new IPEndPoint(IPAddress.Any, Camera.PortRx);
            }
            catch (Exception ex)
            {
                Camera.Updates?.Invoke(null, ex.Message);
            }

            if (Camera.IsMulticast)
            {
                Thread.Sleep(500);
                IPAddress multicastAddress = IPAddress.Parse(Camera.MulticastIP);
                socketRx.JoinMulticastGroup(multicastAddress);
            }
            socketRx.Client.ReceiveTimeout = 1000;
            socketRx.Client.ReceiveBufferSize = (int)(Camera.Payload * Camera.Height * 5);
        }

        private void DecodePackets()
        {
            int packetID = 0;
            int bufferLength = 0;
            byte[] singlePacket;
            try
            {
                singlePacket = socketRx.Receive(ref endPoint);
                while (Camera.IsStreaming)
                {
                    singlePacket = socketRx.Receive(ref endPoint);
                    if (singlePacket.Length > 44) //Packet
                    {
                        packetID = (singlePacket[6] << 8) | singlePacket[7];

                        if (packetID < finalPacketID) //Check for final packet because final packet length maybe lesser than the regular packets
                        {
                            bufferLength = singlePacket.Length - 8;
                            Buffer.BlockCopy(singlePacket, 8, Camera.rawBytes, (packetID - 1) * bufferLength, bufferLength);
                        }
                        else
                        {
                            Buffer.BlockCopy(singlePacket, 8, Camera.rawBytes, (packetID - 1) * bufferLength, singlePacket.Length - 8);
                        }
                    }
                    else if (singlePacket.Length == 16) //Trailer packet size=16, Header Packet Size=44
                    {
                        if (finalPacketID == 0)
                        {
                            finalPacketID = packetID - 1;
                        }

                        if (Camera.frameReadyAction != null)
                        {
                            Camera.frameReadyAction?.Invoke(Camera.rawBytes);
                        }
                        else
                        {
                            Camera.FrameReady?.Invoke(intPtr, Camera.rawBytes);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Camera.Updates?.Invoke(null, ex.Message);
                _ = Camera.StopStream();
            }
        }

        private void RxCpp()
        {
            intPtr = new IntPtr();
            try
            {
                if (Camera.IsRawFrame)
                {
                    if (Environment.Is64BitProcess)
                    {
                        CvInterop64.GetRawFrame(Camera.PortRx, Camera.IsMulticast ? Camera.MulticastIP : null, out intPtr, RawFrameReady);
                    }
                    else
                    {
                        CvInterop.GetRawFrame(Camera.PortRx, Camera.IsMulticast ? Camera.MulticastIP : null, out intPtr, RawFrameReady);
                    }
                }
                else
                {
                    if (Environment.Is64BitProcess)
                    {
                        CvInterop64.GetProcessedFrame(Camera.PortRx, Camera.IsMulticast ? Camera.MulticastIP : null, out intPtr, RawFrameReady);
                    }
                    else
                    {
                        CvInterop.GetProcessedFrame(Camera.PortRx, Camera.IsMulticast ? Camera.MulticastIP : null, out intPtr, RawFrameReady);
                    }
                }
            }
            catch (Exception ex)
            {
                Camera.Updates?.Invoke(null, ex.Message);
                _ = Camera.StopStream();
            }
        }

        private void RawFrameReady(int value)
        {
            try
            {
                Marshal.Copy(intPtr, Camera.rawBytes, 0, Camera.rawBytes.Length);
                if (Camera.frameReadyAction != null)
                {
                    Camera.frameReadyAction?.Invoke(Camera.rawBytes);
                }
                else
                {
                    Camera.FrameReady?.Invoke(intPtr, Camera.rawBytes);
                }
            }
            catch (Exception ex)
            {
                Camera.Updates?.Invoke(null, ex.Message);
            }
        }

        #endregion OldCode
    }
}