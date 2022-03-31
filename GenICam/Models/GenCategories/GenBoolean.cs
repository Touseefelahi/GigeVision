using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenBoolean : GenCategory, IGenBoolean
    {
        public GenBoolean(CategoryProperties categoryProperties, IPValue pValue, Dictionary<string, IMathematical> expressions)
        {
            GetValueCommand = new DelegateCommand(ExecuteGetValueCommand);
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
            CategoryProperties = categoryProperties;
            PValue = pValue;
            Expressions = expressions;
        }

        public bool Value { get; set; }
        public bool ValueToWrite { get; set; }

        public async Task<bool> GetValueAsync()
        {
            Int64? value = null;
            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.WO)
                    value = await Register.GetValueAsync();
            }
            else if (PValue is IntSwissKnife intSwissKnife)
                value = await intSwissKnife.GetValueAsync();

            return value == 1;
        }

        public async void SetValue(bool value)
        {
            if (PValue is IRegister Register)
            {
                var length = Register.Length;
                byte[] pBuffer = new byte[length];
                pBuffer[0] = Convert.ToByte(value);

                var reply = await Register.SetAsync(pBuffer, length);
                if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                    Value = value;
            }

            ValueToWrite = Value;
            RaisePropertyChanged(nameof(ValueToWrite));
        }


        private void ExecuteSetValueCommand()
        {
            SetValue(ValueToWrite);

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