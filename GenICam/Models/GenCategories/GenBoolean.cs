using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenBoolean : GenCategory, IGenBoolean
    {
        public bool Value { get; set; }
        public bool ValueToWrite { get; set; }

        public GenBoolean(CategoryProperties categoryProperties, IPValue pValue, Dictionary<string, IMathematical> expressions)
        {
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
            CategoryProperties = categoryProperties;
            PValue = pValue;
            Expressions = expressions;
            if (CategoryProperties.Visibility != GenVisibility.Invisible)
                SetupFeatures();
        }

        public async Task<bool> GetValue()
        {
            Int64? value = null;
            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.WO)
                    value = await Register.GetValue();

            }
            else if (PValue is IntSwissKnife intSwissKnife)
                value = await intSwissKnife.GetValue();

            if (value == 1)
                Value = true;
            else if (value == 0)
                Value = false;

            return Value;
        }

        public async void SetValue(bool value)
        {
            if (PValue is IRegister Register)
            {
                var length = Register.Length;
                byte[] pBuffer = new byte[length];
                pBuffer[0] = Convert.ToByte(value);

                var reply = await Register.Set(pBuffer, length);
                if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                    Value = value;
            }

            ValueToWrite = Value;
            RaisePropertyChanged(nameof(ValueToWrite));
        }

        public async void SetupFeatures()
        {
            Value = await GetValue();
            ValueToWrite = Value;
        }

        private void ExecuteSetValueCommand()
        {
            SetValue(ValueToWrite);
        }
    }
}