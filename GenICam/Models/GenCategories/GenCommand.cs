using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Commands;

namespace GenICam
{
    /// <summary>
    /// GenICam command implementation.
    /// </summary>
    public class GenCommand : GenCategory, ICommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenCommand"/> class.
        /// </summary>
        /// <param name="categoryProperties">The category properties.</param>
        /// <param name="commandValue">The command value.</param>
        /// <param name="pValue">The PValue.</param>
        public GenCommand(CategoryProperties categoryProperties, long commandValue, IPValue pValue)
                : base(categoryProperties, pValue)
        {
            CommandValue = commandValue;

            // As the Execute method is async and the CommandValue is not, we should wait for the execution.
            SetValueCommand = new DelegateCommand(ExecuteCommand);
        }

        private async void ExecuteCommand()
        {
            await Execute();
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public long Value { get; set; }

        /// <summary>
        /// Gets the command value.
        /// </summary>
        public long CommandValue { get; private set; }

        /// <inheritdoc/>
        public async Task<IReplyPacket> Execute()
        {
            try
            {
                if (PValue is not null)
                {
                    return await PValue.SetValueAsync(CommandValue);
                }
            }
            catch (Exception ex)
            {
                //ToDo: display exception.
            }

            throw new GenICamException(message: $"Unable to set the value, missing register reference", new MissingFieldException());
        }

        /// <inheritdoc/>
        public async Task<bool> IsDone()
        {
            throw new NotImplementedException();
        }
    }
}