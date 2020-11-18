using System;
using System.Collections.Generic;

namespace GenICam
{
    public interface IGenCommand
    {
        void Execute();

        bool IsDone();
    }
}