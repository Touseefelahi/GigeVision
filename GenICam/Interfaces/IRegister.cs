using System;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to an edit box showing a hex string
    /// </summary>
    public interface IRegister : INode, IPValue
    {
        Task<IReplyPacket> SetAsync(byte[] pBuffer, long length);
        Task<byte[]> GetAsync();
        /// <summary>
        /// Register Address in hex format
        /// </summary>
        Task<long?> GetAddressAsync();
        /// <summary>
        /// Register Length
        /// </summary>
        Int64 GetLength();
    }
}