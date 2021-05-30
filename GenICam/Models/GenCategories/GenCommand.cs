using Prism.Commands;
using System;
using System.Collections.Generic;

namespace GenICam
{
    public class GenCommand : GenCategory, IGenCommand
    {
        public GenCommand(CategoryProperties categoryProperties, Int64 commandValue, IPValue pValue, Dictionary<string, IMathematical> expressions)
        {
            CategoryProperties = categoryProperties;
            CommandValue = commandValue;
            PValue = pValue;
            Expressions = expressions;

            SetValueCommand = new DelegateCommand(Execute);
        }

        public Int64 Value { get; set; }
        public Int64 CommandValue { get; private set; }

        public async void Execute()
        {
            if (PValue is IRegister Register)
            {
                var length = Register.Length;
                byte[] pBuffer = new byte[length];

                switch (length)
                {
                    case 2:
                        pBuffer = BitConverter.GetBytes((UInt16)CommandValue);
                        break;

                    case 4:
                        pBuffer = BitConverter.GetBytes((Int32)CommandValue);
                        break;

                    case 8:
                        pBuffer = BitConverter.GetBytes(CommandValue);
                        break;
                }

                await Register.Set(pBuffer, length).ConfigureAwait(false);
            };
        }

        public bool IsDone()
        {
            throw new NotImplementedException();
        }
    }
}