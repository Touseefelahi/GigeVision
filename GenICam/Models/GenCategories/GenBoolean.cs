using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Commands;

namespace GenICam
{
    /// <summary>
    /// GenICam Boolean representation.
    /// </summary>
    public class GenBoolean : GenCategory, IBoolean
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenBoolean"/> class.
        /// </summary>
        /// <param name="categoryProperties">The category properties.</param>
        /// <param name="pValue">the pointeur in the value.</param>
        /// <param name="expressions">The expressions for evaluation.</param>
        public GenBoolean(CategoryProperties categoryProperties, IPValue pValue, Dictionary<string, IMathematical> expressions)
        {
            GetValueCommand = new DelegateCommand(ExecuteGetValueCommand);
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
            CategoryProperties = categoryProperties;
            PValue = pValue;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the value is true or false.
        /// </summary>
        public bool Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the value to write is true or false.
        /// </summary>
        public bool ValueToWrite { get; set; }

        /// <summary>
        /// Gets the value async.
        /// </summary>
        /// <returns>The value as a bool.</returns>
        public async Task<bool> GetValueAsync()
        {
            long? value = null;
            if (PValue is IRegister Register)
            {
                // Keeping this code as need to be implemented
                // if (Register.AccessMode != GenAccessMode.WO)
                // {
                //     value = await Register.GetValueAsync();
                // }
            }
            else if (PValue is IPValue pValue)
            {
                value = await pValue.GetValueAsync();
            }

            return value == 1;
        }

        /// <summary>
        /// Sets the value async.
        /// </summary>
        /// <param name="value">The value as a boolean.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IReplyPacket> SetValueAsync(bool value)
        {
            IReplyPacket reply = null;
            if (PValue is IRegister register)
            {
                var length = register.GetLength();
                byte[] pBuffer = new byte[length];
                pBuffer[0] = Convert.ToByte(value);

                reply = await register.SetAsync(pBuffer, length);
                if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                {
                    Value = value;
                }
            }

            ValueToWrite = Value;
            RaisePropertyChanged(nameof(ValueToWrite));

            return reply;
        }

        private async void ExecuteSetValueCommand()
        {
            await SetValueAsync(ValueToWrite);
        }

        private async void ExecuteGetValueCommand()
        {
            Value = await GetValueAsync();
            ValueToWrite = Value;
            RaisePropertyChanged(nameof(Value));
            RaisePropertyChanged(nameof(ValueToWrite));
        }
    }
}