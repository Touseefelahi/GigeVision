using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenMaskedIntReg : IRegister
    {
        /// <summary>
        /// Register Address in hex format
        /// </summary>
        public Int64 Address { get; private set; }

        /// <summary>
        /// Register Length
        /// </summary>
        public Int64 Length { get; private set; }

        /// <summary>
        /// Register Access Mode
        /// </summary>
        public GenAccessMode AccessMode { get; private set; }

        public Dictionary<string, IntSwissKnife> Expressions { get; set; }
        public IGenPort GenPort { get; }

        public GenMaskedIntReg(long address, long length, GenAccessMode accessMode, Dictionary<string, IntSwissKnife> expressions, IGenPort genPort)
        {
            Expressions = expressions;
            GenPort = genPort;
            Address = address;
            Length = length;
            AccessMode = accessMode;
        }

        public async Task<IReplyPacket> Get(long length)
        {
            return await GenPort.Read(Address, Length);
        }

        public long GetAddress()
        {
            return Address;
        }

        public long GetLength()
        {
            return Length;
        }

        public async Task<IReplyPacket> Set(byte[] pBuffer, long length)
        {
            return await GenPort.Write(pBuffer, Address, length);
        }

        public async Task<long> GetValue()
        {
            var reply = await Get(Length);
            Int64 value = 0;

            await Task.Run(() =>
            {
                if (reply.MemoryValue != null)
                {
                    switch (Length)
                    {
                        case 2:
                            value = BitConverter.ToUInt16(reply.MemoryValue);
                            break;

                        case 4:
                            value = BitConverter.ToUInt32(reply.MemoryValue);
                            break;

                        case 8:
                            value = BitConverter.ToInt64(reply.MemoryValue);
                            break;

                        default:
                            value = BitConverter.ToInt64(reply.MemoryValue);
                            break;
                    }
                }
                else
                {
                    value = (Int64)reply.RegisterValue;
                }
            });

            return value;
        }
    }
}