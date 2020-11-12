using System.Collections.Generic;

namespace GenICam
{
    public interface IGenInteger
    {
        IGenFloat GetFloatAlias();

        long? GetInc();

        IncMode GetIncMode();

        List<long> GetListOfValidValue();

        long GetMax();

        long GetMin();

        Representation GetRepresentation();

        string GetUnit();

        long GetValue();

        void ImposeMax(long max);

        void ImposeMin(long min);

        void SetValue(long value);
    }
}