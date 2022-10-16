using System;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to an edit box showing a hex string.
    /// </summary>
    public interface IRegister : IPValue
    {
        /// <summary>
        /// Set the register bytes async.
        /// </summary>
        /// <param name="pBuffer">The bytes to write.</param>
        /// <param name="length">The lenght of bytes to write.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public Task<IReplyPacket> SetAsync(byte[] pBuffer, long length);

        /// <summary>
        /// Gets the register bytes async.
        /// </summary>
        /// <returns>The register bytes.</returns>
        public Task<byte[]> GetAsync();

        /// <summary>
        /// Gets the register address as a long.
        /// </summary>
        /// <returns>The register address as a long.</returns>
        public Task<long?> GetAddressAsync();

        /// <summary>
        /// Gets the register length.
        /// </summary>
        /// <returns>The length in byte.</returns>
        public long GetLength();
        /// <summary>
        /// Gets the access mode.
        /// </summary>
        public GenAccessMode AccessMode { get; }

    }
}