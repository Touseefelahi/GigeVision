using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Extracts an integer packed into a register, e.g., from bit 8 to bit 12.
    /// </summary>
    public class GenMaskedIntReg : IRegister
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenMaskedIntReg"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="length">The length.</param>
        /// <param name="msb">The MSB value.</param>
        /// <param name="lsb">The LSB value.</param>
        /// <param name="bit">The bit value.</param>
        /// <param name="sign">The sign.</param>
        /// <param name="accessMode">The access mode.</param>
        /// <param name="pAddress">The pointer on the register address.</param>
        /// <param name="genPort">The GenICam port.</param>
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

        /// <summary>
        /// Gets the register address in hex format.
        /// </summary>
        public long? Address { get; private set; }

        /// <summary>
        /// Gets the pointer on the register address.
        /// </summary>
        public object PAddress { get; private set; }

        /// <summary>
        /// Gets the register length.
        /// </summary>
        public long Length { get; private set; }

        /// <summary>
        /// Gets the MSB value.
        /// </summary>
        public short? MSB { get; private set; }

        /// <summary>
        /// Gets the LSB value.
        /// </summary>
        public short? LSB { get; private set; }

        /// <summary>
        /// Gets the bit value.
        /// </summary>
        public byte? Bit { get; private set; }

        /// <summary>
        /// Gets the sign.
        /// </summary>
        public Sign? Sign { get; private set; }

        /// <summary>
        /// Gets the register access mode.
        /// </summary>
        public GenAccessMode AccessMode { get; private set; }

        /// <summary>
        /// Gets or sets the list of expressions.
        /// </summary>
        public Dictionary<string, IMathematical> Expressions { get; set; }

        /// <summary>
        /// Gets the GenICam port.
        /// </summary>
        public IPort GenPort { get; }

        /// <inheritdoc/>
        public async Task<long?> GetAddressAsync()
        {
            if (PAddress is IPValue pValue)
            {
                if (Address != null)
                {
                    return Address + (long)(await pValue.GetValueAsync());
                }
                else
                {
                    return (long)(await pValue.GetValueAsync());
                }
            }

            return Address;
        }

        /// <inheritdoc/>
        public long GetLength()
        {
            return Length;
        }

        /// <inheritdoc/>
        public async Task<IReplyPacket> SetAsync(byte[] pBuffer, long length)
        {
            if (PAddress is IPValue pValue)
            {
                return await GenPort.WriteAsync(pBuffer, (long)await pValue.GetValueAsync(), length);
            }
            else if (Address is long adress)
            {
                return await GenPort.WriteAsync(pBuffer, adress, length);
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<long?> GetValueAsync()
        {
            long? value = null;

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

        /// <summary>
        /// Reads the mask.
        /// </summary>
        /// <param name="registerValue">The register value.</param>
        /// <returns>The mask.</returns>
        public long ReadMask(long registerValue)
        {
            var mask = 0xFFFF0000;
            long value = 0;

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
                {
                    value = Convert.ToInt64(Math.Pow(2, bit));
                }
            }

            return value;
        }

        /// <inheritdoc/>
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
                        pBuffer = BitConverter.GetBytes((ushort)value);
                        break;

                    case 4:
                        pBuffer = BitConverter.GetBytes((int)value);
                        break;

                    case 8:
                        pBuffer = BitConverter.GetBytes(value);
                        break;
                }

                reply = await SetAsync(pBuffer, length);
            }

            return reply;
        }

        /// <inheritdoc/>
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

        private async Task<IReplyPacket> GetAsync(long length)
        {
            if (PAddress is IPValue pValue)
            {
                return await GenPort.ReadAsync((long)await pValue.GetValueAsync(), Length);
            }
            else if (Address is long adress)
            {
                return await GenPort.ReadAsync(adress, Length);
            }

            return null;
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
    }
}