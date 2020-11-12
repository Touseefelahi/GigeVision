using Stira.Socket.Interfaces;
using Stira.Socket.Models;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenPort : Transceiver, IGenPort
    {
        public GenPort(int port)
        {
            IP = "127.0.0.1";
            Port = port;
        }

        public async void Read(byte[] pBuffer, long address, long length)
        {
            var reply = await SendUdpAsync(pBuffer);
            var result = reply.Reply.ToArray();

            unsafe
            {
                fixed (byte* ptr = pBuffer)

                    Marshal.Copy(result, 0, (IntPtr)ptr, (int)length);
            };
        }

        public async void Write(byte[] pBuffer, long address, long length)
        {
            var reply = await SendUdpAsync(pBuffer);
            var result = reply.Reply.ToArray();

            unsafe
            {
                fixed (byte* ptr = pBuffer)

                    Marshal.Copy(result, 0, (IntPtr)ptr, (int)result.Length);
            };
        }
    }
}