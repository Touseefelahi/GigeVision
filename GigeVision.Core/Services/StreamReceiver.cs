using System;
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