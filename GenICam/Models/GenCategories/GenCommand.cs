using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenCommand : GenCategory, ICommand
    {
        public GenCommand(CategoryProperties categoryProperties, Int64 commandValue, IPValue pValue, Dictionary<string, IMathematical> expressions)
        {
            CategoryProperties = categoryProperties;
            CommandValue = commandValue;
            PValue = pValue;

            SetValueCommand = new DelegateCommand(()=>Execute());
        }

        public Int64 Value { get; set; }
        public Int64 CommandValue { get; private set; }

        public async Task Execute()
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

                await Register.SetAsync(pBuffer, length);
            };
        }

        public async Task<bool> IsDone()
        {
            throw new NotImplementedException();
        }
    }
}