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

        double GetMax();

        double GetMin();

        Representation GetRepresentation();

        string GetUnit();

        Task<double> GetValue();

        void ImposeMax(double max);

        void ImposeMin(double min);

        void SetValue(double value);

        DisplayNotation GetDisplayNotation();

        uint GetDisplayPrecision();

        IGenEnumeration GetEnumAlias();
    }
}