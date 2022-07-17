using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GenICam
{
    /// <summary>
    /// Merges the integer’s value, min, max, and increment properties from different nodes
    /// </summary>
    public class GenInteger : GenCategory, IInteger
    {
        public GenInteger(CategoryProperties categoryProperties, long? min, long? max, long? inc, IMathematical pMax, IMathematical pMin, IMathematical pInc, IncrementMode? incMode, Representation representation, long? value, string unit, IPValue pValue)
        {
            CategoryProperties = categoryProperties;

            if (min is not null)
                Min = (long)min;

            if (max is not null)
                Max = (long)max;

            if (inc is not null)
                Inc = (long)inc;

            if (value is not null)
                Value = (long)value;

            IncMode = incMode;
            Representation = representation;

            Unit = unit;

            PValue = pValue;
            PMax = pMax;
            PMin = pMin;

            //Control Commands
            GetValueCommand = new DelegateCommand(ExecuteGetValueCommand);
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
        }

        public Representation Representation { get; private set; }
        public string ValidValuesSet { get; private set; }

        /// <summary>
        /// Integer Minimum Value
        /// </summary>
        public long Min { get; private set; }

        /// <summary>
        /// Integer Maximum Value
        /// </summary>
        public long Max { get; private set; }

        /// <summary>
        /// Integer Increment Value
        /// </summary>
        public long Inc { get; private set; } = 1;
        public bool Streamble { get; set; }
        public IncrementMode? IncMode { get; private set; }

        public long? Value
        {
            get;
            set;
        }

        public List<Int64> ListOfValidValue { get; private set; }
        public string Unit { get; private set; } = string.Empty;
        public long ValueToWrite { get; set; }
        public IMathematical PMax { get; }
        public IMathematical PMin { get; }

        public async Task<long?> GetValueAsync()
        {
            if (PValue is IRegister register)
            {
                if (register.AccessMode != GenAccessMode.WO)
                    return (long)(await PValue.GetValueAsync());
            }

            return Value;
        }

        public async Task<IReplyPacket> SetValueAsync(long value)
        {
            IReplyPacket reply = null;
            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.RO)
                {
                    if ((value % Inc) == 0 && (value <= await GetMaxAsync() && value >= await GetMinAsync()))
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

                        reply = await Register.SetAsync(pBuffer, length);
                        if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                            Value = value;
                    }
                }
            }

            ValueToWrite = (long)Value;
            RaisePropertyChanged(nameof(ValueToWrite));
            return reply;
        }

        public async Task<long> GetMinAsync()
        {
            if (PMin is IRegister register)
            {
                if (register.AccessMode != GenAccessMode.WO)
                    return (long)(await PValue.GetValueAsync());
            }

            return 0;
        }

        public async Task<long> GetMaxAsync()
        {
            throw new NotImplementedException();
        }

        public Int64? GetIncrement()
        {
            if (IncMode == IncrementMode.fixedIncrement)
                return Inc;
            else
                return null;
        }

        public List<Int64> GetListOfValidValue()
        {
            if (IncMode == IncrementMode.listIncrement)
                return ListOfValidValue;
            else
                return null;
        }

        public IncrementMode GetIncrementMode()
        {
            return (IncrementMode)IncMode;
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

        public IFloat GetFloatAlias()
        {
            throw new NotImplementedException();
        }

        private async void ExecuteSetValueCommand()
        {
            if (Value != ValueToWrite)
                await SetValueAsync(ValueToWrite);
        }

        Task<long> IInteger.GetMaxAsync()
        {
            throw new NotImplementedException();
        }

        Task<long> IInteger.GetMinAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IReplyPacket> ImposeMinAsync(long value)
        {
            throw new NotImplementedException();
        }

        public Task<IReplyPacket> ImposeMaxAsync(long value)
        {
            throw new NotImplementedException();
        }
        private async void ExecuteGetValueCommand()
        {
            Value = await GetValueAsync();
            ValueToWrite = (long)Value;
            RaisePropertyChanged(nameof(Value));
            RaisePropertyChanged(nameof(ValueToWrite));

        }
    }
}