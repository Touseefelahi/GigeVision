using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IGenBoolean
    {
        Task<bool> GetValue();

        void SetValue(bool value);
    }
}