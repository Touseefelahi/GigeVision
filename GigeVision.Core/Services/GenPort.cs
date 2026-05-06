using GenICam;
using GigeVision.Core.Enums;
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
                var addressBytes = GetAddressBytes((long)address);
                Array.Reverse(addressBytes);

                if (length == 4 && ((long)address % 4) == 0)
                {
                    reply = await Gvcp.ReadRegisterAsync(addressBytes).ConfigureAwait(false);
                }
                else
                {
                    reply = await Gvcp.ReadMemoryAsync(Gvcp.CameraIp, addressBytes, (ushort)length).ConfigureAwait(false);
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
                if (pBuffer == null)
                {
                    throw new GvcpException(message: "missing write buffer.", new NullReferenceException());
                }

                if (length <= 0 || pBuffer.Length < length)
                {
                    throw new GvcpException(message: "invalid write length.", new ArgumentOutOfRangeException(nameof(length)));
                }

                return await WriteBufferAsync((long)address, pBuffer, (int)length).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new GvcpException(message: "failed to write register.", ex);
            }
            finally
            {
                //await Gvcp.LeaveControl();
            }
        }

        private async Task<GvcpReply> WriteBufferAsync(long address, byte[] buffer, int length)
        {
            GvcpReply lastReply = new();

            for (int offset = 0; offset < length;)
            {
                long chunkAddress = address + offset;
                long alignedAddress = chunkAddress & ~0x3L;
                int byteOffset = (int)(chunkAddress - alignedAddress);
                int bytesToWrite = Math.Min(4 - byteOffset, length - offset);
                byte[] wordBuffer = new byte[4];

                if (byteOffset != 0 || bytesToWrite != 4)
                {
                    GvcpReply currentWord = await Gvcp.ReadRegisterAsync(FormatAddress(alignedAddress)).ConfigureAwait(false);
                    if (currentWord.Status != GvcpStatus.GEV_STATUS_SUCCESS)
                    {
                        return currentWord;
                    }

                    byte[] currentBytes = ToBigEndianBytes(currentWord.RegisterValue);
                    Array.Copy(currentBytes, wordBuffer, wordBuffer.Length);
                }

                Array.Copy(buffer, offset, wordBuffer, byteOffset, bytesToWrite);
                uint valueToWrite = ToUInt32BigEndian(wordBuffer, 0);
                string targetAddress = FormatAddress(alignedAddress);
                lastReply = alignedAddress == chunkAddress && bytesToWrite == 4
                    ? await Gvcp.WriteRegisterAsync(targetAddress, valueToWrite).ConfigureAwait(false)
                    : await Gvcp.WriteMemoryAsync(targetAddress, valueToWrite).ConfigureAwait(false);

                if (lastReply.Status != GvcpStatus.GEV_STATUS_SUCCESS)
                {
                    return lastReply;
                }

                offset += bytesToWrite;
            }

            return lastReply;
        }

        private byte[] GetAddressBytes(Int64 address)
        {
            try
            {
                return BitConverter.GetBytes((Int32)address);
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

        private static string FormatAddress(long address)
        {
            return $"0x{address:X8}";
        }

        private static byte[] ToBigEndianBytes(uint value)
        {
            return new[]
            {
                (byte)((value >> 24) & 0xFF),
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)(value & 0xFF),
            };
        }

        private static uint ToUInt32BigEndian(byte[] buffer, int offset)
        {
            return (uint)((buffer[offset] << 24) |
                (buffer[offset + 1] << 16) |
                (buffer[offset + 2] << 8) |
                buffer[offset + 3]);
        }
    }
}