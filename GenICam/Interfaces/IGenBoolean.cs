using System;
using System.Collections.Generic;

namespace GenICam
{
    public interface IGenBoolean
    {
        bool GetValue();

        void SetVlaue(bool value);
    }
}