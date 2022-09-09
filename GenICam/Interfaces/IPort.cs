using System;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to the camera port and is typically not shown graphically.
    /// </summary>
    public interface IPort
    {
        /// <summary>
        /// Reads async the address for a specific length of bytes.
        /// </summary>
        /// <param name="address">The address to read.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<IReplyPacket> ReadAsync(long? address, long length);

        /// <summary>
        /// Writes async the address for a specific length of bytes.
        /// </summary>
        /// <param name="pBuffer">The buffer to write.</param>
        /// <param name="address">The address to write.</param>
        /// <param name="length">The length of bytes to write.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<IReplyPacket> WriteAsync(byte[] pBuffer, long? address, long length);
    }
}