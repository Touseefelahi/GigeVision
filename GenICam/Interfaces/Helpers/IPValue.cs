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
        Task<long?> GetValueAsync();

        /// <summary>
        /// Sets the value async.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<IReplyPacket> SetValueAsync(long value);
    }
}