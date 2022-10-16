using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to a slider with value, min, and max plus a physical unit.
    /// </summary>
    public interface IFloat : IPValue
    {
        /// <summary>
        /// Gets the value async.
        /// </summary>
        /// <returns>The value as a double.</returns>
        public Task<double?> GetValueAsync();

        /// <summary>
        /// Sets the value async.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>A task.</returns>
        public Task SetValueAsync(double value);

        /// <summary>
        /// Gets the minimum possible value async.
        /// </summary>
        /// <returns>The minimum possible value as a double.</returns>
        public Task<double> GetMinAsync();

        /// <summary>
        /// Gets the maximum possible value async.
        /// </summary>
        /// <returns>The maximum possible value as a double.</returns>
        public Task<double> GetMaxAsync();

        /// <summary>
        /// Gets the increment mode.
        /// </summary>
        /// <returns>Te increment mode.</returns>
        public IncrementMode GetIncrementMode();

        /// <summary>
        /// Gets the increment if GetIncrenmentMode returns fixedIncrement.
        /// </summary>
        /// <returns>The increment is specified.</returns>
        public long? GetIncrement();

        /// <summary>
        /// Gets a list of valid values if GetIncrementMode returns listIncrement.
        /// </summary>
        /// <returns>the list of valid values.</returns>
        public List<double>? GetListOfValidValue();

        /// <summary>
        /// Gets the representation.
        /// </summary>
        /// <returns>The representation.</returns>
        public Representation GetRepresentation();

        /// <summary>
        /// Gets the unit.
        /// </summary>
        /// <returns>A string represneting the unit.</returns>
        public string GetUnit();

        /// <summary>
        /// Determines how to display the float number.
        /// </summary>
        /// <returns>The display notation for the float numbers.</returns>
        public DisplayNotation GetDisplayNotation();

        /// <summary>
        /// Determines the precision to display the float number with.
        /// </summary>
        /// <returns>The display precision.</returns>
        public long GetDisplayPrecision();

        /// <summary>
        /// Gets a node with represents the same value in integer type.
        /// </summary>
        /// <returns>An interger node.</returns>
        public IInteger GetIntAlias();

        /// <summary>
        /// Gets a node with represents the same value in enumeration type.
        /// </summary>
        /// <returns>An enumeration node.</returns>
        public IEnumeration GetEnumAlias();

        /// <summary>
        /// Imposes the minimum value async.
        /// </summary>
        /// <param name="min">The minimum value.</param>
        /// <returns>A task.</returns>
        public Task ImposeMinAsync(long min);

        /// <summary>
        /// Imposes the maximum value async.
        /// </summary>
        /// <param name="max">The maximum value.</param>
        /// <returns>A task.</returns>
        public Task ImposeMaxAsync(long max);
    }
}