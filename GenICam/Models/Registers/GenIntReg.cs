using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenIntReg : IRegister
    {
        public GenIntReg(long? address, long length, GenAccessMode accessMode, Dictionary<string, IMathematical> expressions, object pAddress, IGenPort genPort)
        {
            Address = address;
            PAddress = pAddress;
            Length = length;
            AccessMode = accessMode;
            Expressions = expressions;
            GenPort = genPort;
        }

        /// <summary>
        /// Register Address in hex format
        /// </summary>
        public Int64? Address { get; private set; }

        public object PAddress { get; private set; }

        /// <summary>
        /// Register Length
        /// </summary>
        public Int64 Length { get; private set; }

        /// <summary>
        /// Register Access Mode
        /// </summary>
        public GenAccessMode AccessMode { get; private set; }

        public Dictionary<string, IMathematical> Expressions { get; set; }
        public IGenPort GenPort { get; }

        public async Task<IReplyPacket> Get(long length)
        {
            if (Address is long adress)
                return await GenPort.Read(adress, Length);
            else if (PAddress is IntSwissKnife pAddress)
                return await GenPort.Read(await pAddress.GetValue(), Length);

            return null;
        }

        public async Task<IReplyPacket> Set(byte[] pBuffer, long length)
        {
            if (Address is long adress)
                return await GenPort.Write(pBuffer, adress, length);
            else if (PAddress is IntSwissKnife pAddress)
                return await GenPort.Write(pBuffer, await pAddress.GetValue(), length);

            return null;
        }

        public async Task<long?> GetAddress()
        {
            if (Address is long address)
                return address;
            else if (PAddress is IntSwissKnife swissKnife)
                return (long)(await swissKnife.GetValue());

            return null;
        }

        public long GetLength()
        {
            return Length;
        }

        public async Task<long> GetValue()
        {
            Int64 value;

                var reply = await Get(Length);
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
                    value = Convert.ToInt64(reply.RegisterValue);
                }
            return value;
        }

        public async Task<IReplyPacket> SetValue(long value)
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

                reply = await Set(pBuffer, length);
            }

            return reply;
        }
    }
}