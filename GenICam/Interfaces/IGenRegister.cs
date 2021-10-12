using System;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IGenRegister
    {

        Task<IReplyPacket> Set(byte[] pBuffer, Int64 length);

        Task<long?> GetAddress();
        Int64 GetLength();
    }
}