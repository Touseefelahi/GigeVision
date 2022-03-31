using System;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IPValue : IIsImplemented
    {
        Task<Int64> GetValueAsync();
        Task<IReplyPacket> SetValueAsync(long value);

    }
}