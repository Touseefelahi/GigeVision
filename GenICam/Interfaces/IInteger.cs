using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to a slider with value, min, max, and increment
    /// </summary>
    public interface IInteger : IPValue
    {
        public Task<long> GetValueAsync();
        public Task<IReplyPacket> SetValueAsync(long value);
        Task<long> GetMaxAsync();
        Task<long> GetMinAsync();
        /// <summary>
        /// Returns the type of increment.
        /// </summary>
        /// <returns></returns>
        IncrementMode GetIncrementMode();
        /// <summary>
        /// Returns the increment if GetIncrementMode returns fixedIncrement
        /// </summary>
        /// <returns></returns>
        long? GetIncrement();
        /// <summary>
        /// Returns a list of valid values if GetIncrementMode returns listIncrement
        /// </summary>
        /// <returns></returns>
        List<long>? GetListOfValidValue();
        Representation GetRepresentation();
        /// <summary>
        /// Returns the unit
        /// </summary>
        /// <returns></returns>
        string GetUnit();
        /// <summary>
        /// Restricts the minimum
        /// </summary>
        /// <param name="value"></param>
        Task<IReplyPacket> ImposeMinAsync(long value);
        /// <summary>
        /// Restricts the maximum
        /// </summary>
        /// <param name="value"></param>
        Task<IReplyPacket> ImposeMaxAsync(long value);
        /// <summary>
        /// Returns a node with represents the same value in float type
        /// </summary>
        /// <returns></returns>
        IFloat GetFloatAlias();
    }
}