using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Extracts an integer lying byte-bounded in a register.
    /// </summary>
    public class GenIntReg : IRegister
    {
        private object PAddress { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenIntReg"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="length">The length.</param>
        /// <param name="accessMode">The access mode.</param>
        /// <param name="expressions">The expressions.</param>
        /// <param name="pAddress">The pointer in the address.</param>
        /// <param name="genPort">The GenICam port.</param>
        public GenIntReg(long? address, long length, GenAccessMode accessMode, Dictionary<string, IMathematical> expressions, object pAddress, IPort genPort)
        {
            Address = address;
            PAddress = pAddress;
            Length = length;
            AccessMode = accessMode;
            Expressions = expressions;
            GenPort = genPort;
        }

        /// <inheritdoc/>
        public async Task<byte[]> GetAsync()
        {
            throw new NotImplementedException();
        }

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
        /// Gests or sets the list of expressions.
        /// </summary>
        public Dictionary<string, IMathematical> Expressions { get; set; }

        /// <summary>
        /// Gets the GenICam port.
        /// </summary>
        public IPort GenPort { get; }

        // TODO: check this dead code.
        ////public async Task<IReplyPacket> GetAsync(long length)
        ////{
        ////    if (PAddress is IPValue<IConvertible> pValue)
        ////        return await GenPort.ReadAsync((long)await pValue.GetValueAsync(), Length);
        ////    else if (Address is long adress)
        ////        return await GenPort.ReadAsync(adress, Length);

        ////    return null;
        ////}

        /// <inheritdoc/>
        public async Task<IReplyPacket> SetAsync(byte[] pBuffer, long length)
        {
            return await GenPort.WriteAsync(pBuffer, await GetAddressAsync(), length);
        }

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
        public async Task<long?> GetValueAsync()
        {
            return (await GenPort.ReadAsync(await GetAddressAsync(), Length)).RegisterValue;
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
    }
}