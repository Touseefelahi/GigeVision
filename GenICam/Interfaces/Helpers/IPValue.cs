using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Interface for the PValue.
    /// </summary>
    public interface IPValue
    {
        /// <summary>
        /// Gets the value async.
        /// </summary>
        /// <returns>The value as long.</returns>
        public Task<long?> GetValueAsync();

        /// <summary>
        /// Sets the value async.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public Task<IReplyPacket> SetValueAsync(long value);
    }
}