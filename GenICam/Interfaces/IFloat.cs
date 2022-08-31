using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to a slider with value, min, and max plus a physical unit.
    /// </summary>
    public interface IFloat : IPValue
    {
        public Task<double> GetValueAsync();
        public Task SetValueAsync(double value);
        Task<double> GetMinAsync();
        Task<double> GetMaxAsync();
        IncrementMode GetIncrementMode();
        /// <summary>
        /// Returns the increment if GetIncrenmentMode returns fixedIncrement
        /// </summary>
        /// <returns></returns>
        long? GetIncrement();
        /// <summary>
        /// Returns a list of valid values if GetIncrementMode returns listIncrement
        /// </summary>
        /// <returns></returns>
        List<double>? GetListOfValidValue();
        Representation GetRepresentation();
        string GetUnit();
        /// <summary>
        /// Determines how to display the float number
        /// </summary>
        /// <returns></returns>
        DisplayNotation GetDisplayNotation();
        /// <summary>
        /// Determines the precision to display the float number with
        /// </summary>
        /// <returns></returns>
        long GetDisplayPrecision();
        /// <summary>
        /// Returns a node with represents the same value in integer type
        /// </summary>
        /// <returns></returns>
        IInteger GetIntAlias();
        /// <summary>
        /// Returns a node with represents the same value in enumeration type
        /// </summary>
        /// <returns></returns>
        IEnumeration GetEnumAlias();
        Task ImposeMinAsync(long min);
        Task ImposeMaxAsync(long max);
    }
}