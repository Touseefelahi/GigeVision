using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenICam
{
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

        /// <summary>
        /// Register Access Mode
        /// </summary>
        public GenAccessMode AccessMode { get; private set; }

        public Dictionary<string, IMathematical> Expressions { get; set; }
        public IGenPort GenPort { get; }

        public GenMaskedIntReg(long? address, long length, short? msb, short? lsb, byte? bit, GenAccessMode accessMode, object pAddress, IGenPort genPort)
        {
            GenPort = genPort;
            Address = address;
            Length = length;
            MSB = msb;
            LSB = lsb;
            Bit = bit;
            AccessMode = accessMode;
            PAddress = pAddress;
        }

        private async Task<IReplyPacket> Get(long length)
        {
            if (Address is long adress)
                return await GenPort.Read(adress, Length).ConfigureAwait(false);
            else if (PAddress is IntSwissKnife pAddress)
                return await GenPort.Read(await pAddress.GetValue().ConfigureAwait(false), Length).ConfigureAwait(false);

            return null;
        }

        public async Task<long?> GetAddress()
        {
            if (Address is long address)
                return address;
            else if (PAddress is IntSwissKnife swissKnife)
                return (long)(await swissKnife.GetValue().ConfigureAwait(false));

            return null;
        }

        public long GetLength()
        {
            return Length;
        }

        public async Task<IReplyPacket> Set(byte[] pBuffer, long length)
        {
            if (Address is long adress)
                return await GenPort.Write(pBuffer, adress, length).ConfigureAwait(false);
            else if (PAddress is IntSwissKnife pAddress)
                return await GenPort.Write(pBuffer, await pAddress.GetValue().ConfigureAwait(false), length).ConfigureAwait(false);

            return null;
        }

        public async Task<long> GetValue()
        {
            Int64 value = 0;

            var key = (await GetAddress().ConfigureAwait(false)).ToString();

            var tempValue = await TempDictionary.Get(key).ConfigureAwait(false);
            if (tempValue is not null)
                value = (long)tempValue;
            else
            {
                var reply = await Get(Length).ConfigureAwait(false);

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
                        value = ReadMask(reply.RegisterValue);
                    }
                }).ConfigureAwait(false);
                await TempDictionary.Add(key, value).ConfigureAwait(false);
                
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
                value = (Int64)(registerValue & mask) >> shift;
            }
            else if (Bit is byte bit)
            {
                var bytesValue = BitConverter.GetBytes(registerValue);

                var bits = new BitArray(bytesValue);

                if (bits[bit])
                    value = (long)Math.Pow(2, (bit));
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

                reply = await Set(pBuffer, length).ConfigureAwait(false);
            }

            return reply;
        }
    }
}