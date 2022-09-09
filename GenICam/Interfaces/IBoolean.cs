using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to a check box.
    /// </summary>
    public interface IBoolean
    {
        /// <summary>
        /// Gets the value async.
        /// </summary>
        /// <returns>The value as boolean.</returns>
        public Task<bool> GetValueAsync();

        /// <summary>
        /// Sets the value async.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public Task<IReplyPacket> SetValueAsync(bool value);
    }
}