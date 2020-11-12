using System;
using System.Collections.Generic;

namespace GenICam
{
    public class GenStringReg : GenCategory, IGenString, IGenRegister
    {
        /// <summary>
        /// Register Address in hex format
        /// </summary>
        public Int64 Address { get; private set; }

        /// <summary>
        /// Register Length
        /// </summary>
        public Int64 Length { get; private set; }

        /// <summary>
        /// Register Access Mode
        /// </summary>
        public GenAccessMode AccessMode { get; private set; }

        public string Value { get; set; }

        public GenStringReg(CategoryProperties categoryProperties, Int64 address, ushort length, GenAccessMode accessMode)
        {
            CategoryProperties = categoryProperties;
            Address = address;
            Length = length;
            AccessMode = accessMode;
        }

        public string GetValue()
        {
            return Value;
        }

        public void SetValue(string value)
        {
            Value = value;
        }

        public long GetMaxLength()
        {
            return Length;
        }

        public void Get(byte[] pBuffer, long length)
        {
            throw new NotImplementedException();
        }

        public void Set(byte[] pBuffer, long length)
        {
            throw new NotImplementedException();
        }

        public long GetAddress()
        {
            return Address;
        }

        public long GetLength()
        {
            return Length;
        }
    }
}