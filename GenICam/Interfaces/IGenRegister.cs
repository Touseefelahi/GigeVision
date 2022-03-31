using System;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IGenRegister
    {

        Task<IReplyPacket> SetAsync(byte[] pBuffer, Int64 length);

        Task<long?> GetAddressAsync();
        Task<byte[]> GetAddressBytesAsync();
        Int64 GetLength();
    }
}