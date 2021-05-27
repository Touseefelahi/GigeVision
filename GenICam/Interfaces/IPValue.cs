using System;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IPValue : IIsImplemented
    {
        Task<Int64> GetValue();
        Task<IReplyPacket> SetValue(long value);

    }
}