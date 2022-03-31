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

        Task<long> GetMaxAsync();

        Task<long> GetMinAsync();

        Representation GetRepresentation();

        string GetUnit();

        void ImposeMax(long max);

        void ImposeMin(long min);
    }
}