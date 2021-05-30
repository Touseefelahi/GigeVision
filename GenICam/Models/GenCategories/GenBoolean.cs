using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenBoolean : GenCategory, IGenBoolean
    {
        public GenBoolean(CategoryProperties categoryProperties, IPValue pValue, Dictionary<string, IMathematical> expressions)
        {
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
            CategoryProperties = categoryProperties;
            PValue = pValue;
            Expressions = expressions;
            if (CategoryProperties.Visibility != GenVisibility.Invisible)
                SetupFeatures();
        }

        public bool Value { get; set; }
        public bool ValueToWrite { get; set; }

        public async Task<bool> GetValue()
        {
            Int64? value = null;
            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.WO)
                    value = await Register.GetValue().ConfigureAwait(false);
            }
            else if (PValue is IntSwissKnife intSwissKnife)
                value = await intSwissKnife.GetValue().ConfigureAwait(false);

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

                var reply = await Register.Set(pBuffer, length).ConfigureAwait(false);
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