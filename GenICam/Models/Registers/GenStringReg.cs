using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenStringReg : GenCategory, IGenString, IGenRegister
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

        public IGenPort GenPort { get; }
        public string Value { get; set; }

        public GenStringReg(CategoryProperties categoryProperties, Int64 address, ushort length, GenAccessMode accessMode, IGenPort genPort)
        {
            CategoryProperties = categoryProperties;
            Address = address;
            Length = length;
            AccessMode = accessMode;
            GenPort = genPort;
            SetupFeatures();
        }

        public async Task<string> GetValue()
        {
            var key = (await GetAddress().ConfigureAwait(false)).ToString();

            var tempValue = await TempDictionary.Get(key).ConfigureAwait(false);
            if (tempValue is not null)
                Value = tempValue as string;
            else
            {

            var reply = await Get(Length).ConfigureAwait(false);
            try
            {
                if (!(reply.MemoryValue is null))
                    Value = Encoding.ASCII.GetString(reply.MemoryValue);
                    await TempDictionary.Add(key, Value).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            }
            return Value;
        }

        public async void SetValue(string value)
        {
            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.RO)
                {
                    var length = Register.GetLength();
                    byte[] pBuffer = new byte[length];
                    pBuffer = ASCIIEncoding.ASCII.GetBytes(value);

                    var reply = await Register.Set(pBuffer, length).ConfigureAwait(false);
                    if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                    {
                        if (reply.MemoryValue != null)
                            Value = Encoding.ASCII.GetString(reply.MemoryValue);
                    }
                }
            }
        }

        public long GetMaxLength()
        {
            return Length;
        }

        public async Task<IReplyPacket> Get(long length)
        {
            return await GenPort.Read(Address, Length).ConfigureAwait(false);
        }

        public async Task<IReplyPacket> Set(byte[] pBuffer, long length)
        {
            return await GenPort.Write(pBuffer, Address, length).ConfigureAwait(false);
        }

        public async Task<long?> GetAddress()
        {
            if (Address is long address)
                return address;
            return null;
        }

        public long GetLength()
        {
            return Length;
        }

        public async void SetupFeatures()
        {
            Value = await GetValue().ConfigureAwait(false);
        }
    }
}