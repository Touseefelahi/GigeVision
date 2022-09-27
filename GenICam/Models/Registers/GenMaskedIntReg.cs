using GenICam.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Extracts an integer packed into a register, e.g., from bit 8 to bit 12.
    /// </summary>
    public class GenMaskedIntReg : RegisterBase
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
                           : base(address, length, accessMode, pAddress, genPort)
        {
            MSB = msb;
            LSB = lsb;
            Bit = bit;
            Sign = sign;
        }

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

        public override async Task<long?> GetValueAsync()
        {
            try
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
            catch (Exception ex)
            {
                throw new GenICamException(message: $"Failed to cast the value type", ex);
            }
        }

        private async Task<IReplyPacket> GetAsync(long length)
        {
            try
            {

                if (await GetAddressAsync() is long address)
                {
                    return await GenPort.ReadAsync(address, length);
                }

                throw new GenICamException(message: $"Unable to get the value, missing register address", new MissingFieldException());
            }
            catch (Exception ex)
            {
                throw new GenICamException(message: $"Failed to get the register value ", ex);
            }
        }

        /// <summary>
        /// Reads the mask.
        /// </summary>
        /// <param name="registerValue">The register value.</param>
        /// <returns>The mask.</returns>
        public long ReadMask(long registerValue)
        {
            try
            {
                var mask = 0xFFFF0000;

                if (MSB is short msb && LSB is short lsb)
                {
                    var msbMask = mask >> msb;
                    var lsbMask = mask >> lsb;
                    mask = msbMask | lsbMask;
                    var shift = (short)((Length * 8) - 1) - lsb;
                    return Convert.ToInt64((registerValue & mask) >> shift);
                }
                else if (Bit is byte bit)
                {
                    var bytesValue = BitConverter.GetBytes(registerValue);

                    var bits = new BitArray(bytesValue);

                    if (bits[bit])
                    {
                        return Convert.ToInt64(Math.Pow(2, bit));
                    }

                    return 0;
                }

                throw new GenICamException(message: $"Unable to read the mask value", new InvalidOperationException());
            }
            catch (Exception ex)
            {
                throw new GenICamException(message: $"Unable to read the mask value", ex);
            }
        }

        private long ConvertBytesToLong(byte[] valueBytes)
        {
            try
            {
                switch (Length)
                {
                    case 2:
                        return BitConverter.ToUInt16(valueBytes);

                    case 4:
                        return BitConverter.ToUInt32(valueBytes);

                    default:
                        return BitConverter.ToInt64(valueBytes);
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new GenICamException(message: "Failed to convert the given array to integer", ex);
            }
        }
    }
}