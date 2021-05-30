using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GenICam
{
    public class GenInteger : GenCategory, IGenInteger, IPValue
    {
        public GenInteger(CategoryProperties categoryProperties, long min, long max, long inc, IncMode incMode, Representation representation, long value, string unit, IPValue pValue, Dictionary<string, IMathematical> expressions)
        {
            CategoryProperties = categoryProperties;
            Min = min;
            if (max == 0)
                max = Int32.MaxValue;

            Max = max;
            if (inc == 0)
                inc = 1;

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

        public GenInteger(long value, IPValue pValue = null)
        {
            Value = value;
            PValue = pValue;
        }

        public bool PIsLocked { get; internal set; }

        public Representation Representation { get; internal set; }

        /// <summary>
        /// Integer Minimum Value
        /// </summary>
        public Int64 Min { get; private set; } = long.MinValue;

        /// <summary>
        /// Integer Maximum Value
        /// </summary>
        public Int64 Max { get; private set; } = long.MaxValue;

        /// <summary>
        /// Integer Increment Value
        /// </summary>
        public Int64 Inc { get; private set; } = 1;

        public IncMode IncMode { get; private set; }

        public long Value
        {
            get;
            set;
        }

        public List<Int64> ListOfValidValue { get; private set; }
        public string Unit { get; private set; }
        public long ValueToWrite { get; set; }

        public async Task<Int64> GetValue()
        {
            if (PValue is IRegister register)
            {
                if (register.AccessMode != GenAccessMode.WO)
                    return await PValue.GetValue().ConfigureAwait(false);
            }
            else if (PValue is IntSwissKnife intSwissKnife)
            {
                return await intSwissKnife.GetValue().ConfigureAwait(false);
            }

            return Value;
        }

        public async Task<IReplyPacket> SetValue(Int64 value)
        {
            IReplyPacket reply = null;
            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.RO)
                {
                    if ((value % Inc) == 0)
                    {
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
            }
            ValueToWrite = Value;
            RaisePropertyChanged(nameof(ValueToWrite));
            return reply;
        }

        public async Task<Int64> GetMin()
        {
            var pMin = await ReadIntSwissKnife("pMin").ConfigureAwait(false);
            if (pMin != null)
                return (Int64)pMin;

            return Min;
        }

        public async Task<Int64> GetMax()
        {
            var pMax = await ReadIntSwissKnife("pMax").ConfigureAwait(false);
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

        public async void SetupFeatures()
        {
            Value = await GetValue();
            Max = await GetMax();
            Min = await GetMin();
            ValueToWrite = Value;
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
                await SetValue(ValueToWrite).ConfigureAwait(false);
        }
    }
}