using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to a slider with value, min, max, and increment.
    /// </summary>
    public interface IInteger : IPValue
    {
        /// <summary>
        /// Gets the value async.
        /// </summary>
        /// <returns>The value as a long.</returns>
         Task<long?> GetValueAsync();

        /// <summary>
        /// Sets the value async.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>A reply packet.</returns>
         Task<IReplyPacket> SetValueAsync(long value);

        /// <summary>
        /// Gets the maximum value async.
        /// </summary>
        /// <returns>The maximum value as a long.</returns>
         Task<long> GetMaxAsync();

        /// <summary>
        /// Gets the minimum value async.
        /// </summary>
        /// <returns>The minimum value as a long.</returns>
         Task<long> GetMinAsync();

        /// <summary>
        /// Gets the type of increment.
        /// </summary>
        /// <returns>The increment mode.</returns>
         IncrementMode GetIncrementMode();

        /// <summary>
        /// Gets the increment if GetIncrementMode returns fixedIncrement.
        /// </summary>
        /// <returns>The increment if available.</returns>
         long? GetIncrement();

        /// <summary>
        /// Returns a list of valid values if GetIncrementMode returns listIncrement.
        /// </summary>
        /// <returns>A list of values if available.</returns>
         List<long> GetListOfValidValue();

        /// <summary>
        /// Gets the representation.
        /// </summary>
        /// <returns>The representation.</returns>
         Representation GetRepresentation();

        /// <summary>
        /// Gets the unit.
        /// </summary>
        /// <returns>The unit.</returns>
         string GetUnit();

        /// <summary>
        /// Restricts the minimum.
        /// </summary>
        /// <param name="value">The value to impose.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
         Task<IReplyPacket> ImposeMinAsync(long value);

        /// <summary>
        /// Restricts the maximum.
        /// </summary>
        /// <param name="value">The value to impose.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
         Task<IReplyPacket> ImposeMaxAsync(long value);

        /// <summary>
        /// Gets the node with represents the same value in float type.
        /// </summary>
        /// <returns>The float node.</returns>
         IFloat GetFloatAlias();
    }
}