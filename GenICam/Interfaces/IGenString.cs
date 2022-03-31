using System;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IGenString
    {
        Task<string> GetValueAsync();

        Task SetValueAsync(string value);

        Int64 GetMaxLength();
    }
}