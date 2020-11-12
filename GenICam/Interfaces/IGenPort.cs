using System;
using System.Collections.Generic;

namespace GenICam
{
    public interface IGenPort
    {
        void Read(byte[] pBuffer, Int64 address, Int64 length);

        void Write(byte[] pBuffer, Int64 address, Int64 length);
    }
}