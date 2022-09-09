using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to an edit box showing a string.
    /// </summary>
    public interface IString
    {
        /// <summary>
        /// Gets the value async.
        /// </summary>
        /// <returns>The value as a string.</returns>
        Task<string> GetValueAsync();

        /// <summary>
        /// Sets the value async.
        /// </summary>
        /// <param name="value">The string to set.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<IReplyPacket> SetValueAsync(string value);

        /// <summary>
        /// Gets the maximum length of the string.
        /// </summary>
        /// <returns>The maximum length of the string.</returns>
        long GetMaxLength();
    }
}