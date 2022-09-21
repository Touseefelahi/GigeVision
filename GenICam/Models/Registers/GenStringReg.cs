using System;
using System.IO;
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
            try
            {
                var reply = await Get(Length);
                Value = Encoding.ASCII.GetString(reply.MemoryValue);
            }
            catch (DecoderFallbackException ex)
            {
                throw new GenICamException(message: "Failed to get the register value", ex);
            }
            catch (ArgumentNullException ex)
            {
                throw new GenICamException(message: "Failed to get the register value", ex);
            }
            catch (ArgumentException ex)
            {
                throw new GenICamException(message: "Failed to get the register value", ex);
            }

            return Value;
        }

        /// <inheritdoc/>
        public async Task<IReplyPacket> SetValueAsync(string value)
        {
            try
            {
                if (PValue is IRegister register)
                {
                    return await SetStringValue(value, register);
                }

                return await SetStringValue(value);
            }
            catch (Exception ex)
            {
                throw new GenICamException(message: "Failed to set  the string register value", ex);
            }
        }

        private async Task<IReplyPacket> SetStringValue(string value, IRegister register)
        {
            if (register.AccessMode != GenAccessMode.RO)
            {
                var length = register.GetLength();
                var pBuffer = ASCIIEncoding.ASCII.GetBytes(value);

                var reply = await register.SetAsync(pBuffer, length);
                Value = Encoding.ASCII.GetString(reply.MemoryValue);
                return reply;
            }

            throw new GenICamException(message: $"Unable to set the register value; it's read only", new AccessViolationException());
        }

        private async Task<IReplyPacket> SetStringValue(string value)
        {
            if (AccessMode != GenAccessMode.RO)
            {
                var length = GetLength();
                var pBuffer = ASCIIEncoding.ASCII.GetBytes(value);

                var reply = await SetAsync(pBuffer, length);
                Value = Encoding.ASCII.GetString(reply.MemoryValue);
                return reply;
            }

            throw new GenICamException(message: $"Unable to set the register value; it's read only", new AccessViolationException());
        }

        /// <inheritdoc/>
        public long GetMaxLength() => Length;

        /// <summary>
        /// Gets the reply packet.
        /// </summary>
        /// <param name="length">The length of bytes to read.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IReplyPacket> Get(long length) => await GenPort.ReadAsync(Address, length);

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
            try
            {
                if (await GetAddressAsync() is long address)
                {
                    var addressBytes = BitConverter.GetBytes(address);
                    Array.Reverse(addressBytes);
                    return addressBytes;
                }

                throw new GenICamException(message: "Failed to get the register address", new InvalidDataException());
            }
            catch (Exception ex)
            {
                throw new GenICamException(message: "Failed to get the register address", ex);
            }
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