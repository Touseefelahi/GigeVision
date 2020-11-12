using System;
using System.Collections.Generic;

namespace GenICam
{
    public class GenFloat : GenCategory, IGenFloat
    {
        public double Min { get; private set; }
        public double Max { get; set; }
        public Int64 Inc { get; private set; } = 1;
        public IncMode IncMode { get; private set; }
        public Representation Representation { get; private set; }
        public double Value { get; private set; }
        public List<double> ListOfValidValue { get; private set; }
        public string Unit { get; private set; }
        public DisplayNotation DisplayNotation { get; private set; }
        public uint DisplayPrecision { get; private set; }
        public Dictionary<string, IPRegister> Registers { get; internal set; }

        public GenFloat(CategoryProperties categoryProperties, double min, double max, long inc, IncMode incMode, Representation representation, double value, string unit, Dictionary<string, IPRegister> registers)
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
            return Max;
        }

        public double GetMin()
        {
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

        public double GetValue()
        {
            return Value;
        }

        public void ImposeMax(double max)
        {
            Max = max;
        }

        public void ImposeMin(double min)
        {
            Min = min;
        }

        public void SetValue(double value)
        {
            Value = value;
        }
    }
}