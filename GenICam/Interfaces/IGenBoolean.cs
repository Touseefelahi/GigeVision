using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IGenBoolean
    {
        Task<bool> GetValueAsync();

        void SetValue(bool value);
    }
}