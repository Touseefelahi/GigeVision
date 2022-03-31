using Prism.Commands;
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
        public string ValueToWrite { get; set; }

        public GenStringReg(CategoryProperties categoryProperties, Int64 address, ushort length, GenAccessMode accessMode, IGenPort genPort)
        {
            CategoryProperties = categoryProperties;
            Address = address;
            Length = length;
            AccessMode = accessMode;
            GenPort = genPort;
            GetValueCommand = new DelegateCommand(ExecuteGetValueCommand);
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
        }

        private async void ExecuteGetValueCommand()
        {
            Value = await GetValueAsync();
            ValueToWrite = Value;
            RaisePropertyChanged(nameof(Value));
            RaisePropertyChanged(nameof(ValueToWrite));
        }

        private async void ExecuteSetValueCommand()
        {
            if (Value != ValueToWrite)
                await SetValueAsync(ValueToWrite);
        }

        public async Task<string> GetValueAsync()
        {
            var reply = await Get(Length);
            try
            {
                if (!(reply.MemoryValue is null))
                    Value = Encoding.ASCII.GetString(reply.MemoryValue);
            }
            catch (Exception ex)
            {
                throw ex;
            }  
            return Value;
        }

        public async Task SetValueAsync(string value)
        {
            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.RO)
                {
                    var length = Register.GetLength();
                    byte[] pBuffer = new byte[length];
                    pBuffer = ASCIIEncoding.ASCII.GetBytes(value);

                    var reply = await Register.SetAsync(pBuffer, length);
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
            return await GenPort.ReadAsync(Address, Length);
        }

        public async Task<IReplyPacket> SetAsync(byte[] pBuffer, long length)
        {
            return await GenPort.WriteAsync(pBuffer, Address, length);
        }

        public async Task<long?> GetAddressAsync()
        {
            if (Address is long address)
                return address;
            return null;
        }

        public long GetLength()
        {
            return Length;
        }

        public async Task<byte[]> GetAddressBytesAsync()
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