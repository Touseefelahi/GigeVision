using System.Collections.Generic;
using System.Threading.Tasks;

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

        Task<long> GetValue();

        void ImposeMax(long max);

        void ImposeMin(long min);

        void SetValue(long value);
    }
}