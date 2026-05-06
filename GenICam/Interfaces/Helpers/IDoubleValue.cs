using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Interface for values that can be represented as doubles.
    /// </summary>
    public interface IDoubleValue
    {
        /// <summary>
        /// Gets the value as a double.
        /// </summary>
        /// <returns>The value as a double when available.</returns>
        public Task<double?> GetDoubleValueAsync();

        /// <summary>
        /// Sets the value as a double.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>The reply packet from the underlying write path.</returns>
        public Task<IReplyPacket> SetDoubleValueAsync(double value);
    }
}