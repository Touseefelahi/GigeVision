using GenICam;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using System;
using System.Threading.Tasks;

namespace GigeVision.Core
{
    public class GenPort : IPort
    {
        public GenPort(IGvcp gvcp)
        {
            Gvcp = gvcp;
        }

        public IGvcp Gvcp { get; }

        public async Task<IReplyPacket> ReadAsync(long? address, long length)
        {
            if (address is null)
            {
                throw new GvcpException(message: "missing address.", new NullReferenceException());
            }

            try
            {
                GvcpReply reply;
                var addressBytes = GetAddressBytes((long)address, length);
                Array.Reverse(addressBytes);

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
            catch (Exception ex)
            {
                throw new GvcpException(message: "failed to read register.", ex);
            }
        }

        public async Task<IReplyPacket> WriteAsync(byte[] pBuffer, long? address, long length)
        {
            if (address is null)
            {
                throw new GvcpException(message: "missing address.", new NullReferenceException());
            }
            try
            {
                var addressBytes = GetAddressBytes((long)address, length);
                Array.Reverse(addressBytes);
                //await Gvcp.TakeControl(false);
                return await Gvcp.WriteRegisterAsync(addressBytes, BitConverter.ToUInt32(pBuffer)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new GvcpException(message: "failed to read register.", ex);
            }
            finally
            {
                //await Gvcp.LeaveControl();
            }
        }

        private byte[] GetAddressBytes(Int64 address, Int64 length)
        {
            try
            {
                switch (length)
                {
                    case 2:
                        return BitConverter.GetBytes((Int16)address);

                    case 4:
                        return BitConverter.GetBytes((Int32)address);

                    default:
                        return BitConverter.GetBytes((Int32)address);
                }
            }
            catch (InvalidCastException ex)
            {
                throw new GvcpException(message: "failed to cast address value.", ex);
            }
            catch (Exception ex)
            {
                throw new GvcpException(message: "failed to address value.", ex);
            }
        }
    }
}