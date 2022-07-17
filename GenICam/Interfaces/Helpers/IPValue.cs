using System;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IPValue
    {
        Task<long?> GetValueAsync();
        Task<IReplyPacket> SetValueAsync(long value);

    }
}