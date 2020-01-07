using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GigeVision.Core
{
    public static class CvInterop
    {
#if DEBUG
#if WIN32
        private const string libraryPath = "lib\\StreamRxCpp.dll";
#else
        private const string libraryPath = "lib\\StreamRxCpp.dll";
#endif
#else
#if WIN32
        private const string libraryPath = "lib\\StreamRxCpp.dll";
#else
        private const string libraryPath = "lib\\StreamRxCpp.dll";
#endif
#endif

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void ProgressCallback(int value);

        [DllImport(libraryPath, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Start(int port, out IntPtr imageDataAddress, int width, int height, int packetLengthToSet,
        [MarshalAs(UnmanagedType.FunctionPtr)] ProgressCallback frameReady);

        [DllImport(libraryPath, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Stop();

        [DllImport(libraryPath, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong GetCurrentInvalidFrameCounter();

        [DllImport(libraryPath, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong GetCurrentValidFrameCounter();
    }
}