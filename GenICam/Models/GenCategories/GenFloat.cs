using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenFloat : GenCategory, IGenFloat
    {
        public double Min { get; private set; }
        public double Max { get; set; }
        public Int64 Inc { get; private set; } = 1;
        public IncMode IncMode { get; private set; }
        public Representation Representation { get; private set; }
        public double Value { get; set; }
        public List<double> ListOfValidValue { get; private set; }
        public string Unit { get; private set; }
        public DisplayNotation DisplayNotation { get; private set; }
        public uint DisplayPrecision { get; private set; }
        public double ValueToWrite { get; set; }

        public GenFloat(CategoryProperties categoryProperties, double min, double max, long inc, IncMode incMode, Representation representation, double value, string unit, IPValue pValue, Dictionary<string, IntSwissKnife> expressions)
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
            if (CategoryProperties.Visibility != GenVisibility.Invisible)
                SetupFeatures();
        }

        public IGenFloat GetFloatAlias()
        {
            throw new NotImplementedException();
        }

        public DisplayNotation GetDisplayNotation()
        {
            return DisplayNotation;
        }

        public uint GetDisplayPrecision()
        {
            return DisplayPrecision;
        }

        public IGenEnumeration GetEnumAlias()
        {
            throw new NotImplementedException();
        }

        public long? GetInc()
        {
            if (IncMode == IncMode.fixedIncrement)
                return Inc;
            else
                return null;
        }

        public IncMode GetIncMode()
        {
            return IncMode;
        }

        public IGenInteger GetIntAlias()
        {
            throw new NotImplementedException();
        }

        public List<double> GetListOfValidValue()
        {
            if (IncMode == IncMode.listIncrement)
                return ListOfValidValue;
            else
                return null;
        }

        public double GetMax()
        {
            var pMax = ReadIntSwissKnife("pMax");
            if (pMax != null) Max = (double)pMax;

            return Max;
        }

        public double GetMin()
        {
            var pMin = ReadIntSwissKnife("pMin");
            if (pMin != null) Min = (double)pMin;

            return Min;
        }

        public Representation GetRepresentation()
        {
            return Representation; ;
        }

        public string GetUnit()
        {
            throw new NotImplementedException();
        }

        public async Task<double> GetValue()
        {
            double value = Value;

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

        public async void SetValue(double value)
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

        public void ImposeMax(double max)
        {
            Max = max;
        }

        public void ImposeMin(double min)
        {
            Min = min;
        }

        public async void SetupFeatures()
        {
            Value = await GetValue();
            Max = GetMax();
            Min = GetMin();
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

        private void ExecuteSetValueCommand()
        {
            if (Value != ValueToWrite)
            {
                SetValue(ValueToWrite);
            }
        }
    }
}