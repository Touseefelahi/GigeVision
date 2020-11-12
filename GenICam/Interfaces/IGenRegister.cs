using System;

namespace GenICam
{
    public interface IGenRegister
    {
        void Get(byte[] pBuffer, Int64 length);

        void Set(byte[] pBuffer, Int64 length);

        Int64 GetAddress();

        Int64 GetLength();
    }
}