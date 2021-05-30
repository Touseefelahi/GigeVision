using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenFloat : GenCategory, IGenFloat, IPValue
    {
        public GenFloat(CategoryProperties categoryProperties, double min, double max, long inc, IncMode incMode, Representation representation, double value, string unit, IPValue pValue, Dictionary<string, IMathematical> expressions)
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

        public async Task<double> GetMax()
        {
            var pMax = await ReadIntSwissKnife("pMax").ConfigureAwait(false);
            if (pMax != null) Max = (double)pMax;

            return Max;
        }

        public async Task<double> GetMin()
        {
            var pMin = await ReadIntSwissKnife("pMin").ConfigureAwait(false);
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

        public async Task<long> GetValue()
        {
            double value = Value;

            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.WO)
                {
                    value = await Register.GetValue().ConfigureAwait(false);

                    byte[] pBuffer = BitConverter.GetBytes(value);

                    if (Representation == Representation.HexNumber)
                        Array.Reverse(pBuffer);

                    switch (pBuffer.Length)
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
            else if (PValue is IntSwissKnife intSwissKnife)
            {
                value = await intSwissKnife.GetValue().ConfigureAwait(false);
            }

            return (long)value;
        }

        public async Task<IReplyPacket> SetValue(long value)
        {
            IReplyPacket reply = null;

            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.RO)
                {
                    if ((value % Inc) != 0)
                        return null;

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

                    reply = await Register.Set(pBuffer, length).ConfigureAwait(false);
                    if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                        Value = value;
                }
            }

            ValueToWrite = Value;
            RaisePropertyChanged(nameof(ValueToWrite));
            return reply;
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
            Max = await GetMax();
            Min = await GetMin();
        }

        public async void SetValue(double value)
        {
            await SetValue((long)value).ConfigureAwait(false);
        }

        private async Task<Int64?> ReadIntSwissKnife(string pNode)
        {
            if (Expressions == null)
                return null;

            if (!Expressions.ContainsKey(pNode))
                return null;

            var pValueNode = Expressions[pNode];
            if (pValueNode is IntSwissKnife intSwissKnife)
            {
                return await intSwissKnife.GetValue().ConfigureAwait(false);
            }

            return null;
        }

        private async void ExecuteSetValueCommand()
        {
            if (Value != ValueToWrite)
            {
                await SetValue((long)ValueToWrite).ConfigureAwait(false);
            }
        }
    }
}