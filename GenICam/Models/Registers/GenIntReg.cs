using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GenICam
{
    public class GenIntReg : IGenRegister, IPRegister
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
        public IGenPort Port { get; }

        public GenIntReg(long address, long length, GenAccessMode accessMode, Dictionary<string, IPRegister> registers)
        {
            Address = address;
            Length = length;
            AccessMode = accessMode;
            Registers = registers;
            Port = new GenPort(2020);
        }

        public void Get(byte[] pBuffer, long length)
        {
            Port.Read(pBuffer, Address, Length);
        }

        public void Set(byte[] pBuffer, long length)
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

        public long GetValue()
        {
            byte[] pBuffer = new byte[Length];
            Get(pBuffer, Length);

            switch (Length)
            {
                case 2:
                    return BitConverter.ToUInt16(pBuffer);

                case 4:
                    return BitConverter.ToUInt32(pBuffer);

                case 8:
                    return BitConverter.ToInt64(pBuffer);
            }

            return BitConverter.ToInt64(pBuffer);

        }
    }
}