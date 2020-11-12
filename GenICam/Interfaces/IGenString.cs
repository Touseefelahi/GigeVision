using System;

namespace GenICam
{
    public interface IGenString
    {
        string GetValue();

        void SetValue(string value);

        Int64 GetMaxLength();
    }
}