using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenFloat : GenCategory, IFloat
    {
        public GenFloat(CategoryProperties categoryProperties, double min, double max, long inc, IncrementMode incMode, Representation representation, double value, string unit, IPValue pValue, Dictionary<string, IMathematical> expressions)
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
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
            GetValueCommand = new DelegateCommand(ExecuteGetValueCommand);
        }

        private async void ExecuteGetValueCommand()
        {
            Value = (long)await GetValueAsync();
            ValueToWrite = Value;
            RaisePropertyChanged(nameof(Value));
            RaisePropertyChanged(nameof(ValueToWrite));
        }

        public double Min { get; private set; }
        public double Max { get; set; }
        public Int64 Inc { get; private set; } = 1;
        public IncrementMode IncMode { get; private set; }
        public Representation Representation { get; private set; }
        public double Value { get; set; }
        public List<double> ListOfValidValue { get; private set; }
        public string Unit { get; private set; }
        public DisplayNotation DisplayNotation { get; private set; }
        public uint DisplayPrecision { get; private set; }
        public double ValueToWrite { get; set; }

        public IFloat GetFloatAlias()
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

        public IEnumeration GetEnumAlias()
        {
            throw new NotImplementedException();
        }

        public long? GetIncrement()
        {
            if (IncMode == IncrementMode.fixedIncrement)
                return Inc;
            else
                return null;
        }

        public IncrementMode GetIncrementMode()
        {
            return IncMode;
        }

        public IInteger GetIntAlias()
        {
            throw new NotImplementedException();
        }

        public List<double> GetListOfValidValue()
        {
            if (IncMode == IncrementMode.listIncrement)
                return ListOfValidValue;
            else
                return null;
        }

        public async Task<double> GetMaxAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<double> GetMinAsync()
        {
            throw new NotImplementedException();
        }

        public Representation GetRepresentation()
        {
            return Representation; ;
        }

        public string GetUnit()
        {
            throw new NotImplementedException();
        }

        public async Task<long?> GetValueAsync()
        {
            if (PValue is IRegister register)
            {
                //if (register.AccessMode != GenAccessMode.WO)
                //{
                //    byte[] pBuffer = BitConverter.GetBytes(await register.GetValueAsync());

                //    if (Representation == Representation.HexNumber)
                //        Array.Reverse(pBuffer);

                //    switch (pBuffer.Length)
                //    {
                //        case 2:
                //            return BitConverter.ToUInt16(pBuffer); ;

                //        case 4:
                //            return BitConverter.ToUInt32(pBuffer);

                //        case 8:
                //            return BitConverter.ToInt64(pBuffer);

                //    }
                //}
            }
            else if (PValue is IPValue pValue)
            {
                return await pValue.GetValueAsync();
            }

            return (long)Value;
        }

        public async Task<IReplyPacket> SetValueAsync(long value)
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

                    reply = await Register.SetAsync(pBuffer, length);
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

        private async void ExecuteSetValueCommand()
        {
            if (Value != ValueToWrite)
            {
                await SetValueAsync(ValueToWrite);
            }
        }

        Task<double> IFloat.GetValueAsync()
        {
            throw new NotImplementedException();
        }

        public Task SetValueAsync(double value)
        {
            throw new NotImplementedException();
        }

        long IFloat.GetDisplayPrecision()
        {
            throw new NotImplementedException();
        }

        public Task ImposeMinAsync(long min)
        {
            throw new NotImplementedException();
        }

        public Task ImposeMaxAsync(long max)
        {
            throw new NotImplementedException();
        }
    }
}