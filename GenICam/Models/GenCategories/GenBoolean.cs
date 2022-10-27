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
        public GenBoolean(CategoryProperties categoryProperties, IPValue pValue)
        : base(categoryProperties, pValue)
        {
            GetValueCommand = new DelegateCommand(ExecuteGetValueCommand);
            SetValueCommand = new DelegateCommand<object>(ExecuteSetValueCommand);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the value is true or false.
        /// </summary>
        public bool Value { get; set; }

        /// <summary>
        /// Gets the value async.
        /// </summary>
        /// <returns>The value as a bool.</returns>
        public async Task<bool> GetValueAsync()
        {
            if (PValue is not null)
            {
                var value = await PValue.GetValueAsync();
                return value == 1;
            }

            throw new GenICamException(message: $"Unable to get the value, missing register reference", new MissingFieldException());
        }

        /// <summary>
        /// Sets the value async.
        /// </summary>
        /// <param name="value">The value as a boolean.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IReplyPacket> SetValueAsync(bool value)
        {
            if (PValue is not null)
            {
                var valueInByte = Convert.ToByte(value);
                return await PValue.SetValueAsync(valueInByte); ;
            }

            throw new GenICamException(message: $"Unable to set the value, missing register reference", new MissingFieldException());
        }

        private async void ExecuteSetValueCommand(object value)
        {
            try
            {
                await SetValueAsync((bool)value);
                ExecuteGetValueCommand();
            }
            catch (Exception ex)
            {
                //ToDo: display exception.
            }
        }

        private async void ExecuteGetValueCommand()
        {
            try
            {
                Value = await GetValueAsync();
                RaisePropertyChanged(nameof(Value));
            }
            catch (Exception ex)
            {
                //ToDo: display exception.
            }
        }
    }
}