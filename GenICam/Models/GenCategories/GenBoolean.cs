using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenBoolean : GenCategory, IBoolean
    {
        public GenBoolean(CategoryProperties categoryProperties, IPValue pValue, Dictionary<string, IMathematical> expressions)
        {
            GetValueCommand = new DelegateCommand(ExecuteGetValueCommand);
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
            CategoryProperties = categoryProperties;
            PValue = pValue;
        }

        public bool Value { get; set; }
        public bool ValueToWrite { get; set; }

        public async Task<bool> GetValueAsync()
        {
            Int64? value = null;
            if (PValue is IRegister Register)
            {
                //if (Register.AccessMode != GenAccessMode.WO)
                    //value = await Register.GetValueAsync();
            }
            else if (PValue is IPValue pValue)
                value = await pValue.GetValueAsync();

            return value == 1;
        }

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
                    Value = value;
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