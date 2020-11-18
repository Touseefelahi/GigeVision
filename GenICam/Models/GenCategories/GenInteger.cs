using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GenICam
{
    public class GenInteger : GenCategory, IGenInteger, IPValue
    {
        public bool PIsLocked { get; internal set; }

        public Representation Representation { get; internal set; }

        /// <summary>
        /// Integer Minimum Value
        /// </summary>
        public Int64 Min { get; private set; }

        /// <summary>
        /// Integer Maximum Value
        /// </summary>
        public Int64 Max { get; private set; }

        /// <summary>
        /// Integer Increment Value
        /// </summary>
        public Int64 Inc { get; private set; } = 1;

        public IncMode IncMode { get; private set; }

        public Int64 Value
        {
            get;
            set;
        }

        public List<Int64> ListOfValidValue { get; private set; }
        public string Unit { get; private set; }
        public long ValueToWrite { get; set; }

        public GenInteger(CategoryProperties categoryProperties, long min, long max, long inc, IncMode incMode, Representation representation, long value, string unit, IPValue pValue, Dictionary<string, IntSwissKnife> expressions)
        {
            CategoryProperties = categoryProperties;
            Min = min;
            Max = max;
            Inc = inc;
            IncMode = incMode;
            Representation = representation;
            Value = value;
            Unit = unit;
            PValue = pValue;
            Expressions = expressions;
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);

            SetupFeatures();
        }

        public GenInteger(long value)
        {
            Value = value;
        }

        public async Task<Int64> GetValue()
        {
            Int64 value = Value;

            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.WO)
                {
                    var length = Register.GetLength();
                    var reply = await Register.Get(length);

                    byte[] pBuffer;

                    if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                    {
                        if (reply.MemoryValue != null)
                            pBuffer = reply.MemoryValue;
                        else
                            pBuffer = BitConverter.GetBytes(reply.RegisterValue);

                        if (Representation == Representation.HexNumber)
                            Array.Reverse(pBuffer);

                        switch (length)
                        {
                            case 2:
                                value = BitConverter.ToUInt16(pBuffer); ;
                                break;

                            case 4:
                                value = BitConverter.ToUInt32(pBuffer);
                                break;

                            case 8:
                                value = BitConverter.ToInt64(pBuffer);
                                break;
                        }
                    }
                }
            }
            else if (PValue is IntSwissKnife intSwissKnife)
            {
                value = (Int64)intSwissKnife.Value;
            }

            return value;
        }

        public async void SetValue(Int64 value)
        {
            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.RO)
                {
                    if ((value % Inc) != 0)
                        return;

                    var length = Register.GetLength();
                    byte[] pBuffer = new byte[length];

                    switch (length)
                    {
                        case 2:
                            pBuffer = BitConverter.GetBytes((UInt16)value);
                            break;

                        case 4:
                            pBuffer = BitConverter.GetBytes((Int32)value);
                            break;

                        case 8:
                            pBuffer = BitConverter.GetBytes(value);
                            break;
                    }

                    var reply = await Register.Set(pBuffer, length);
                    if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                        Value = value;
                }
            }
            ValueToWrite = Value;
            RaisePropertyChanged(nameof(ValueToWrite));
        }

        public Int64 GetMin()
        {
            var pMin = ReadIntSwissKnife("pMin");
            if (pMin != null)
                return (Int64)pMin;

            return Min;
        }

        public Int64 GetMax()
        {
            var pMax = ReadIntSwissKnife("pMax");
            if (pMax != null)
                return (Int64)pMax;

            return Max;
        }

        public Int64? GetInc()
        {
            if (IncMode == IncMode.fixedIncrement)
                return Inc;
            else
                return null;
        }

        public List<Int64> GetListOfValidValue()
        {
            if (IncMode == IncMode.listIncrement)
                return ListOfValidValue;
            else
                return null;
        }

        public IncMode GetIncMode()
        {
            return IncMode;
        }

        public Representation GetRepresentation()
        {
            return Representation;
        }

        public string GetUnit()
        {
            return Unit;
        }

        public void ImposeMin(Int64 min)
        {
            throw new NotImplementedException();
        }

        public void ImposeMax(Int64 max)
        {
            throw new NotImplementedException();
        }

        public IGenFloat GetFloatAlias()
        {
            throw new NotImplementedException();
        }

        private Int64? ReadIntSwissKnife(string pNode)
        {
            if (Expressions == null)
                return null;

            if (!Expressions.ContainsKey(pNode))
                return null;

            var pValueNode = Expressions[pNode];
            if (pValueNode is IntSwissKnife intSwissKnife)
            {
                return (Int64)intSwissKnife.Value;
            }

            return null;
        }

        public async void SetupFeatures()
        {
            Value = await GetValue();
            Max = GetMax();
            Min = GetMin();
            ValueToWrite = Value;
        }

        private void ExecuteSetValueCommand()
        {
            if (Value != ValueToWrite)
                SetValue(ValueToWrite);
        }
    }
}