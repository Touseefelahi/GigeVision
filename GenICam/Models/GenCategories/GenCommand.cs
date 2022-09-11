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
        {
            CategoryProperties = categoryProperties;
            CommandValue = commandValue;
            PValue = pValue;

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
        public async Task Execute()
        {
            if (PValue is IRegister register)
            {
                var length = register.GetLength();
                byte[] pBuffer = new byte[length];

                switch (length)
                {
                    case 2:
                        pBuffer = BitConverter.GetBytes((ushort)CommandValue);
                        break;

                    case 4:
                        pBuffer = BitConverter.GetBytes((int)CommandValue);
                        break;

                    case 8:
                        pBuffer = BitConverter.GetBytes(CommandValue);
                        break;
                }

                await register.SetAsync(pBuffer, length);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsDone()
        {
            throw new NotImplementedException();
        }
    }
}