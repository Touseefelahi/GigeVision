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

        public GenBoolean(CategoryProperties categoryProperties, IPValue pValue, Dictionary<string, IntSwissKnife> expressions)
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
            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.WO)
                {
                    Int64 value = 0;
                    var length = Register.GetLength();
                    byte[] pBuffer = new byte[length];

                    var reply = await Register.Get(length);

                    if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                    {
                        if (reply.MemoryValue != null)
                            pBuffer = reply.MemoryValue;
                        else
                            pBuffer = BitConverter.GetBytes(reply.RegisterValue);

                        switch (length)
                        {
                            case 2:
                                value = BitConverter.ToUInt16(pBuffer);
                                break;

                            case 4:
                                value = BitConverter.ToUInt32(pBuffer);
                                break;

                            case 8:
                                value = BitConverter.ToInt64(pBuffer);
                                break;
                        }
                    }
                    if (value == 1)
                        return true;
                    else
                        return false;
                }
            }
            else if (PValue is IntSwissKnife intSwissKnife)
            {
                if (intSwissKnife.Value == 1)
                    return true;
                else
                    return false;
            }

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