using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam.Models
{
    public class RegisterBase : IRegister
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterBase"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="length">The length.</param>
        /// <param name="accessMode">The access mode.</param>
        /// <param name="expressions">The expressions.</param>
        /// <param name="pAddress">The pointer in the address.</param>
        /// <param name="genPort">The GenICam port.</param>
        public RegisterBase(long? address, long length, GenAccessMode accessMode, object pAddress, IPort genPort)
        {
            Address = address;
            PAddress = pAddress;
            Length = length;
            AccessMode = accessMode;
            GenPort = genPort;
        }

        internal object PAddress { get; set; }

        /// <summary>
        /// Gets the register address in hex format.
        /// </summary>
        public long? Address { get; private set; }

        /// <summary>
        /// Gets the register Length.
        /// </summary>
        public long Length { get; private set; }

        /// <summary>
        /// Gets the register access mode.
        /// </summary>
        public GenAccessMode AccessMode { get; private set; }

        /// <summary>
        /// Gets the GenICam port.
        /// </summary>
        public IPort GenPort { get; }

        public virtual async Task<byte[]> GetAsync()
        {
            throw new NotImplementedException();
        }

        public virtual async Task<IReplyPacket> SetAsync(byte[] pBuffer, long length)
        {
            return await GenPort.WriteAsync(pBuffer, await GetAddressAsync(), length);
        }

        public virtual async Task<long?> GetAddressAsync()
        {
            try
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

                if (Address is not null)
                {
                    return Address;
                }

                throw new GenICamException(message: $"Failed to get the address value", new InvalidOperationException());
            }
            catch (InvalidCastException ex)
            {
                throw new GenICamException(message: $"Failed to cast the value type", ex);
            }

        }

        public virtual long GetLength()
        {
            return Length;
        }

        public virtual async Task<long?> GetValueAsync()
        {
            return (await GenPort.ReadAsync(await GetAddressAsync(), Length)).RegisterValue;
        }

        public virtual async Task<IReplyPacket> SetValueAsync(long value)
        {
            try
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

                    return await SetAsync(pBuffer, length);
            }
            catch (Exception ex)
            {
                throw new GenICamException(message: $"Failed to cast the value type", ex);
            }
        }
    }
}
