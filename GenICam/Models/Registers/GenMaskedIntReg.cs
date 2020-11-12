using System;
using System.Collections.Generic;

namespace GenICam
{
    public class GenMaskedIntReg : IGenRegister, IPRegister
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
        public GenAccessMode AccessMode { get; set; }

        public Dictionary<string, IPRegister> Registers { get; set; }

        public GenMaskedIntReg(long address, long length, GenAccessMode accessMode, Dictionary<string, IPRegister> registers)
        {
            Registers = registers;
            Address = address;
            Length = length;
            AccessMode = accessMode;
        }

        public void Get(byte[] pBuffer, long length)
        {
            throw new System.NotImplementedException();
        }

        public long GetAddress()
        {
            return Address;
        }

        public long GetLength()
        {
            return Length;
        }

        public void Set(byte[] pBuffer, long length)
        {
            throw new System.NotImplementedException();
        }

        public long GetValue()
        {
            byte[] pBuffer = new byte[Length];
            Get(pBuffer, Length);

            return BitConverter.ToInt64(pBuffer);
        }
    }
}