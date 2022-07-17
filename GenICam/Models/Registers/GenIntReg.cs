using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Extracts an integer lying byte-bounded in a register
    /// </summary>
    public class GenIntReg : IRegister
    {
        public GenIntReg(long? address, long length, GenAccessMode accessMode, Dictionary<string, IMathematical> expressions, object pAddress, IPort genPort)
        {
            Address = address;
            PAddress = pAddress;
            Length = length;
            AccessMode = accessMode;
            Expressions = expressions;
            GenPort = genPort;
        }
        /// <summary>
        /// Gets the register’s content to a buffer
        /// </summary>
        /// <returns></returns>
        public async Task<byte[]> GetAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Register Address in hex format
        /// </summary>
        public Int64? Address { get; private set; }

        private object PAddress { get; set; }

        /// <summary>
        /// Register Length
        /// </summary>
        public Int64 Length { get; private set; }

        /// <summary>
        /// Register Access Mode
        /// </summary>
        public GenAccessMode AccessMode { get; private set; }

        public Dictionary<string, IMathematical> Expressions { get; set; }
        public IPort GenPort { get; }
        //public async Task<IReplyPacket> GetAsync(long length)
        //{
        //    if (PAddress is IPValue<IConvertible> pValue)
        //        return await GenPort.ReadAsync((long)await pValue.GetValueAsync(), Length);
        //    else if (Address is long adress)
        //        return await GenPort.ReadAsync(adress, Length);

        //    return null;
        //}

        public async Task<IReplyPacket> SetAsync(byte[] pBuffer, long length)
        {
            return await GenPort.WriteAsync(pBuffer, await GetAddressAsync(), length);
        }

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

        public long GetLength()
        {
            return Length;
        }

        public async Task<long?> GetValueAsync()
        {
            return (await GenPort.ReadAsync(await GetAddressAsync(), Length)).RegisterValue;
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

    }
}