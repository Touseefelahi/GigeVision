using GenICam;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using System;
using System.Threading.Tasks;

namespace GigeVision.Core
{
    public class GenPort : IGenPort
    {
        public GenPort(IGvcp gvcp)
        {
            Gvcp = gvcp;
        }

        public IGvcp Gvcp { get; }

        public async Task<IReplyPacket> Read(long address, long length)
        {
            GvcpReply reply = new();            
            var addressBytes = GetAddressBytes(address, length);
            Array.Reverse(addressBytes);

            if (length < 4)
                return reply;

            if (length >= 8)
            {
                reply = await Gvcp.ReadMemoryAsync(Gvcp.CameraIp, addressBytes, (ushort)length).ConfigureAwait(false);
            }
            else
            {
                reply = await Gvcp.ReadRegisterAsync(addressBytes).ConfigureAwait(false);
            }

            return reply;
        }

        public async Task<IReplyPacket> Write(byte[] pBuffer, long address, long length)
        {
            await Gvcp.TakeControl(false).ConfigureAwait(false);

            var addressBytes = GetAddressBytes(address, length);
            Array.Reverse(addressBytes);
            return await Gvcp.WriteRegisterAsync(addressBytes, BitConverter.ToUInt32(pBuffer)).ConfigureAwait(false);
        }

        private byte[] GetAddressBytes(Int64 address, Int64 length)
        {
            switch (length)
            {
                case 2:
                    return BitConverter.GetBytes((Int16)address);

                case 4:
                    return BitConverter.GetBytes((Int32)address);

                default:
                    break;
            }
            return BitConverter.GetBytes((Int32)address);
        }
    }
}