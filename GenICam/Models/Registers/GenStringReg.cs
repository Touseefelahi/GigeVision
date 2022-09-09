using System;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;

namespace GenICam
{
    /// <summary>
    /// GenICam String register implementation.
    /// </summary>
    public class GenStringReg : GenCategory, IString
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenStringReg"/> class.
        /// </summary>
        /// <param name="categoryProperties">The Category properties.</param>
        /// <param name="address">The address.</param>
        /// <param name="length">The length.</param>
        /// <param name="accessMode">The access mode.</param>
        /// <param name="genPort">The GenICam port.</param>
        public GenStringReg(CategoryProperties categoryProperties, long address, ushort length, GenAccessMode accessMode, IPort genPort)
        {
            CategoryProperties = categoryProperties;
            Address = address;
            Length = length;
            AccessMode = accessMode;
            GenPort = genPort;
            GetValueCommand = new DelegateCommand(ExecuteGetValueCommand);
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
        }

        /// <summary>
        /// Gets register Address in hex format.
        /// </summary>
        public long Address { get; private set; }

        /// <summary>
        /// Gets register Length.
        /// </summary>
        public long Length { get; private set; }

        /// <summary>
        /// Gets the GenICam Port.
        /// </summary>
        public IPort GenPort { get; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the value to write.
        /// </summary>
        public string ValueToWrite { get; set; }

        /// <inheritdoc/>
        public async Task<string> GetValueAsync()
        {
            var reply = await Get(Length);
            try
            {
                if (!(reply.MemoryValue is null))
                {
                    Value = Encoding.ASCII.GetString(reply.MemoryValue);
                }
            }
            catch (Exception)
            {
                throw;
            }

            return Value;
        }

        /// <inheritdoc/>
        public async Task<IReplyPacket> SetValueAsync(string value)
        {
            IReplyPacket reply = null;
            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.RO)
                {
                    var length = Register.GetLength();
                    byte[] pBuffer = new byte[length];
                    pBuffer = ASCIIEncoding.ASCII.GetBytes(value);

                    reply = await Register.SetAsync(pBuffer, length);
                    if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                    {
                        if (reply.MemoryValue != null)
                        {
                            Value = Encoding.ASCII.GetString(reply.MemoryValue);
                        }
                    }
                }
            }

            return reply;
        }

        /// <inheritdoc/>
        public long GetMaxLength() => Length;

        /// <summary>
        /// Gets the reply packet.
        /// </summary>
        /// <param name="length">The length of bytes to read.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IReplyPacket> Get(long length) => await GenPort.ReadAsync(Address, Length);

        /// <summary>
        /// Sets the bytes for a specific length.
        /// </summary>
        /// <param name="pBuffer">The buffer of bytes to set.</param>
        /// <param name="length">The length of bytes to write.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IReplyPacket> SetAsync(byte[] pBuffer, long length)
        {
            return await GenPort.WriteAsync(pBuffer, Address, length);
        }

        /// <summary>
        /// Gets the address async.
        /// </summary>
        /// <returns>The address as a long.</returns>
        public async Task<long?> GetAddressAsync() => Address;

        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <returns>The length.</returns>
        public long GetLength() => Length;

        /// <summary>
        /// Gets the register bytes async.
        /// </summary>
        /// <returns>The byte array.</returns>
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
            {
                await SetValueAsync(ValueToWrite);
            }
        }
    }
}