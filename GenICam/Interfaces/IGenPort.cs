using System;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IGenPort
    {
        Task<IReplyPacket> Read(Int64 address, Int64 length);

        Task<IReplyPacket> Write(byte[] pBuffer, Int64 address, Int64 length);
    }
}