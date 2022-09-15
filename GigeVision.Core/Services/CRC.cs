using System;
using System.Text;

namespace Crc
{
    public enum InitialCrcValue
    {
        Zeros,
        NonZero1 = 0xffff,
        NonZero2 = 0x1D0F
    }

    /// <summary>
    /// Simple Crc function I found <see href="http://www.sanity-free.com/133/crc_16_ccitt_in_csharp.html">here</see>
    /// </summary>
    public class Crc16Ccitt
    {
        private const ushort poly = 4129;
        private ushort[] table = new ushort[256];
        private ushort initialValue = 0;

        public ushort ComputeChecksum(string inputString)
        {
            return ComputeChecksum(Encoding.ASCII.GetBytes(inputString));
        }

        public ushort ComputeChecksum(byte[] bytes)
        {
            ushort crc = this.initialValue;
            for (int i = 0; i < bytes.Length; ++i)
            {
                crc = (ushort)((crc << 8) ^ table[((crc >> 8) ^ (0xff & bytes[i]))]);
            }
            return crc;
        }

        public byte[] ComputeChecksumBytes(byte[] bytes)
        {
            ushort crc = ComputeChecksum(bytes);
            return BitConverter.GetBytes(crc);
        }

        public Crc16Ccitt(InitialCrcValue initialValue = InitialCrcValue.Zeros)
        {
            this.initialValue = (ushort)initialValue;
            ushort temp, a;
            for (int i = 0; i < table.Length; ++i)
            {
                temp = 0;
                a = (ushort)(i << 8);
                for (int j = 0; j < 8; ++j)
                {
                    if (((temp ^ a) & 0x8000) != 0)
                    {
                        temp = (ushort)((temp << 1) ^ poly);
                    }
                    else
                    {
                        temp <<= 1;
                    }
                    a <<= 1;
                }
                table[i] = temp;
            }
        }
    }
}