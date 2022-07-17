using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Extracts an integer packed into a register, e.g., from bit 8 to bit 12
    /// </summary>
    public class GenMaskedIntReg : IRegister
    {
        /// <summary>
        /// Register Address in hex format
        /// </summary>
        public Int64? Address { get; private set; }
        public object PAddress { get; private set; }

        /// <summary>
        /// Register Length
        /// </summary>
        public Int64 Length { get; private set; }
        public short? MSB { get; private set; }
        public short? LSB { get; private set; }
        public byte? Bit { get; private set; }
        public Sign? Sign { get; private set; }

        /// <summary>
        /// Register Access Mode
        /// </summary>
        public GenAccessMode AccessMode { get; private set; }

        public Dictionary<string, IMathematical> Expressions { get; set; }
        public IPort GenPort { get; }

        public GenMaskedIntReg(long? address, long length, short? msb, short? lsb, byte? bit, Sign? sign, GenAccessMode accessMode, object pAddress, IPort genPort)
        {
            GenPort = genPort;
            Address = address;
            Length = length;
            MSB = msb;
            LSB = lsb;
            Bit = bit;
            Sign = sign;
            AccessMode = accessMode;
            PAddress = pAddress;
        }

        private async Task<IReplyPacket> GetAsync(long length)
        {
            if (PAddress is IPValue pValue)
                return await GenPort.ReadAsync((long)await pValue.GetValueAsync(), Length);
            else if (Address is long adress)
                return await GenPort.ReadAsync(adress, Length);

            return null;
        }

        public async Task<long?> GetAddressAsync()
        {
            if (PAddress is IPValue pValue)
                return (long)(await pValue.GetValueAsync());
            else if (Address is long address)
                return address;
            return null;
        }

        public long GetLength()
        {
            return Length;
        }

        public async Task<IReplyPacket> SetAsync(byte[] pBuffer, long length)
        {
            if (PAddress is IPValue pValue)
                return await GenPort.WriteAsync(pBuffer, (long)await pValue.GetValueAsync(), length);
            else if (Address is long adress)
                return await GenPort.WriteAsync(pBuffer, adress, length);

            return null;
        }

        public async Task<long?> GetValueAsync()
        {
            Int64? value = null;

            var key = (await GetAddressAsync()).ToString();

            var reply = await GetAsync(Length);

            await Task.Run(() =>
            {
                if (reply.MemoryValue != null)
                {
                    value = ConvertBytesToLong(reply.MemoryValue);
                }
                else
                {
                    value = ReadMask(reply.RegisterValue);
                }
            });
            return value;
        }

        private long ConvertBytesToLong(byte[] valueBytes)
        {
            long value;
            switch (Length)
            {
                case 2:
                    value = BitConverter.ToUInt16(valueBytes);
                    break;

                case 4:
                    value = BitConverter.ToUInt32(valueBytes);
                    break;

                case 8:
                    value = BitConverter.ToInt64(valueBytes);
                    break;

                default:
                    value = BitConverter.ToInt64(valueBytes);
                    break;
            }

            return value;
        }

        public long ReadMask(long registerValue)
        {
            var mask = 0xFFFF0000;
            Int64 value = 0;

            if (MSB is short msb && LSB is short lsb)
            {
                var msbMask = mask >> msb;
                var lsbMask = mask >> lsb;
                mask = msbMask | lsbMask;
                var shift = (short)((Length * 8) - 1) - lsb;
                value = Convert.ToInt64((registerValue & mask) >> shift);
            }
            else if (Bit is byte bit)
            {
                var bytesValue = BitConverter.GetBytes(registerValue);

                var bits = new BitArray(bytesValue);

                if (bits[bit])
                    value = Convert.ToInt64(Math.Pow(2, (bit)));
            }

            return value;
        }

        public async Task<IReplyPacket> SetValueAsync(long value)
        {
            IReplyPacket reply = null;
            if (AccessMode != GenAccessMode.RO)
            {
                var length = GetLength();
                byte[] pBuffer = new byte[length];

                switch (length)
                {
                    case 2:
                        pBuffer = BitConverter.GetBytes((UInt16)value);
                        break;

                    case 4:
                        pBuffer = BitConverter.GetBytes((Int32)value);
                        break;

                    case 8:
                        pBuffer = BitConverter.GetBytes(value);
                        break;
                }

                reply = await SetAsync(pBuffer, length);
            }

            return reply;
        }

        public async Task<byte[]> GetAsync()
        {
            byte[] addressBytes = Array.Empty<byte>();
            if (await GetAddressAsync() is long address)
            {
                addressBytes = BitConverter.GetBytes(address);
                Array.Reverse(addressBytes);
            }

            return addressBytes;
        }
    }
}