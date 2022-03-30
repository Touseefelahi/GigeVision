using System;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IGenString
    {
        Task<string> GetValue();

        Task SetValue(string value);

        Int64 GetMaxLength();
    }
}