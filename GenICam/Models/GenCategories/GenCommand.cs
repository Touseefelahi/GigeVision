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
        /// <param name="expressions">The expressions.</param>
        public GenCommand(CategoryProperties categoryProperties, long commandValue, IPValue pValue, Dictionary<string, IMathematical> expressions)
                : base(categoryProperties, pValue)
        {
            CommandValue = commandValue;

            // As the Execute method is async and the CommandValue is not, we should wait for the execution.
            SetValueCommand = new DelegateCommand(() => Execute().GetAwaiter().GetResult());
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
            if (PValue is IPValue pValue)
            {
                return await pValue.SetValueAsync(CommandValue);
            }

            throw new GenICamException(message: $"Unable to get the value, missing register reference", new MissingFieldException());
        }

        /// <inheritdoc/>
        public async Task<bool> IsDone()
        {
            throw new NotImplementedException();
        }
    }
}