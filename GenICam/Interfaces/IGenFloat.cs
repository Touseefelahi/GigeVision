using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IGenFloat
    {
        IGenInteger GetIntAlias();

        long? GetInc();

        IncMode GetIncMode();

        List<double> GetListOfValidValue();

        Task<double> GetMax();

        Task<double> GetMin();

        Representation GetRepresentation();

        string GetUnit();

        Task<long> GetValue();

        void ImposeMax(double max);

        void ImposeMin(double min);

        void SetValue(double value);

        DisplayNotation GetDisplayNotation();

        uint GetDisplayPrecision();

        IGenEnumeration GetEnumAlias();
    }
}