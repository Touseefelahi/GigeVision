using System;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IGenRegister
    {
        Task<IReplyPacket> Get(Int64 length);

        Task<IReplyPacket> Set(byte[] pBuffer, Int64 length);

        Int64 GetAddress();

        Int64 GetLength();
    }
}