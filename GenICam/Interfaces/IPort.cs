using System;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to the camera port and is typically not shown graphically
    /// </summary>
    public interface IPort
    {
        Task<IReplyPacket> ReadAsync(long? address, long length);

        Task<IReplyPacket> WriteAsync(byte[] pBuffer, long? address, long length);
    }
}