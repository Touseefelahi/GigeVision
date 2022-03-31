using System;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IGenPort
    {
        Task<IReplyPacket> ReadAsync(Int64 address, Int64 length);

        Task<IReplyPacket> WriteAsync(byte[] pBuffer, Int64 address, Int64 length);
    }
}