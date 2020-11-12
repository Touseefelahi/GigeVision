using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GenICam
{
    public class GenInteger : GenCategory, IGenInteger, IPRegister
    {
        public bool PIsLocked { get; internal set; }

        public Representation Representation { get; internal set; }

        public Dictionary<string, IPRegister> Registers { get; internal set; }

        /// <summary>
        /// Integer Minimum Value
        /// </summary>
        public Int64 Min { get; private set; }

        /// <summary>
        /// Integer Maximum Value
        /// </summary>
        public Int64 Max { get; set; }

        /// <summary>
        /// Integer Increment Value
        /// </summary>
        public Int64 Inc { get; private set; } = 1;

        public IncMode IncMode { get; private set; }

        public Int64 Value { get; private set; }
        public List<Int64> ListOfValidValue { get; private set; }
        public string Unit { get; private set; }

        public GenInteger(CategoryProperties categoryProperties, long min, long max, long inc, IncMode incMode, Representation representation, long value, string unit, Dictionary<string, IPRegister> registers)
        {
            CategoryProperties = categoryProperties;
            Min = min;
            Max = max;
            Inc = inc;
            IncMode = incMode;
            Representation = representation;
            Value = value;
            Unit = unit;
            Registers = registers;
        }

        public GenInteger(long value)
        {
            Value = value;
        }

        public Int64 GetValue()
        {
            var pValueNode = Registers["pValue"];
            if (pValueNode != null)
            {
                if (pValueNode is IGenRegister register)
                {
                    var length = register.GetLength();
                    var address = register.GetAddress();

                    byte[] pBuffer = new byte[length];

                    register.Get(pBuffer, length);
                    switch (length)
                    {
                        case 2:
                            Value = BitConverter.ToUInt16(pBuffer);
                            break;

                        case 4:
                            Value = BitConverter.ToUInt32(pBuffer);
                            break;

                        case 8:
                            Value = BitConverter.ToInt64(pBuffer);
                            break;
                    }
                }
            }

            return Value;
        }

        public void SetValue(Int64 value)
        {
            Value = value;
        }

        public Int64 GetMin()
        {
            return Min;
        }

        public Int64 GetMax()
        {
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
            Min = min;
        }

        public void ImposeMax(Int64 max)
        {
            Max = max;
        }

        public IGenFloat GetFloatAlias()
        {
            throw new NotImplementedException();
        }

        private void SetupPFeatures()
        {
            foreach (var pFeature in PFeatures)
            {
                switch (pFeature.Key)
                {
                    case "pValue":

                        break;

                    default:
                        break;
                }
            }
        }
    }
}