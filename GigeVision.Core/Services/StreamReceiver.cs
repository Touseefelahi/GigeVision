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
        private IntPtr intPtr;

        private UdpClient socketRx;

        private IPEndPoint endPoint;

        /// <summary>
        /// Receives the GigeStream
        /// </summary>
        /// <param name="camera"></param>
        public StreamReceiver(Camera camera)
        {
            Camera = camera;
        }

        /// <summary>
        /// It starts Thread using C++ library
        /// </summary>
        public void StartRxCppThread()
        {
            Thread threadDecode = new Thread(RxCpp)
            {
                Priority = ThreadPriority.Highest,
                Name = "Decode Cpp Packets Thread",
                IsBackground = true
            };
            threadDecode.Start();
        }

        /// <summary>
        /// Start Rx thread using .Net
        /// </summary>
        public void StartRxThread()
        {
            Thread threadDecode = new Thread(DecodePackets)
            {
                Priority = ThreadPriority.Highest,
                Name = "Decode Packets Thread",
                IsBackground = true
            };
            SetupSocketRx();
            threadDecode.Start();
        }

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
                socketRx = new UdpClient(Camera.port);
                endPoint = new IPEndPoint(IPAddress.Any, Camera.port);
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
            socketRx.Client.ReceiveTimeout = 3000;
            socketRx.Client.ReceiveBufferSize = (int)(Camera.Payload * Camera.Height * 5);
        }

        private void DecodePackets()
        {
            int packetID = 0;
            int finalPacketID = 0;
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
                        CvInterop64.GetRawFrame(Camera.port, Camera.IsMulticast ? Camera.MulticastIP : null, out intPtr, RawFrameReady);
                    }
                    else
                    {
                        CvInterop.GetRawFrame(Camera.port, Camera.IsMulticast ? Camera.MulticastIP : null, out intPtr, RawFrameReady);
                    }
                }
                else
                {
                    if (Environment.Is64BitProcess)
                    {
                        CvInterop64.GetProcessedFrame(Camera.port, Camera.IsMulticast ? Camera.MulticastIP : null, out intPtr, RawFrameReady);
                    }
                    else
                    {
                        CvInterop.GetProcessedFrame(Camera.port, Camera.IsMulticast ? Camera.MulticastIP : null, out intPtr, RawFrameReady);
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
    }
}