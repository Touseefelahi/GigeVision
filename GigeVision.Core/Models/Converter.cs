using GigeVision.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigeVision.Core.Models
{
    public static class Converter
    {
        public static byte[] RegisterStringToByteArray(string registerAddress)
        {
            registerAddress = registerAddress.Replace("0x", "");
            if (registerAddress.Length > 8) throw new RegisterConversionException($"Length Miss match {registerAddress}");
            if (registerAddress.Length % 2 == 1)
            {
                registerAddress = "0" + registerAddress;
            }
            if (registerAddress.Length < 8)
            {
                registerAddress = registerAddress.PadLeft(8, '0');
            }
            try
            {
                var register = Enumerable.Range(0, registerAddress.Length)
                          .Where(x => x % 2 == 0)
                          .Select(x => Convert.ToByte(registerAddress.Substring(x, 2), 16))
                          .ToArray();
                return register;
            }
            catch (Exception ex)
            {
                throw new RegisterConversionException($"Unknown error in conversion {registerAddress}", ex);
            }
        }

        public static uint IpToNumber(string dottedIpAddress)
        {
            uint num = 0;
            if (dottedIpAddress?.Length == 0)
            {
                return 0;
            }
            else
            {
                var splitIpAddress = dottedIpAddress.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                int i;
                for (i = splitIpAddress.Length - 1; i >= 0; i--)
                {
                    num += ((uint.Parse(splitIpAddress[i]) % 256) * (uint)Math.Pow(256, 3 - i));
                }
                return num;
            }
        }

        public static byte[] RegisterStringsToByteArray(string[] registerAddresses)
        {
            byte[] registersBytes = new byte[4 * registerAddresses.Length];
            int registerIndex = 0;
            foreach (var register in registerAddresses)
            {
                var registerBytes = RegisterStringToByteArray(register);
                registersBytes[0 + (registerIndex * 4)] = registerBytes[0];
                registersBytes[1 + (registerIndex * 4)] = registerBytes[1];
                registersBytes[2 + (registerIndex * 4)] = registerBytes[2];
                registersBytes[3 + (registerIndex * 4)] = registerBytes[3];
                registerIndex++;
            }
            return registersBytes;
        }

        public static byte[] HexStringToByteArray(string registerAddress)
        {
            registerAddress = registerAddress.Replace("0x", "");
            if (registerAddress.Length % 2 == 1)
            {
                registerAddress = "0" + registerAddress;
            }
            try
            {
                var register = Enumerable.Range(0, registerAddress.Length)
                          .Where(x => x % 2 == 0)
                          .Select(x => Convert.ToByte(registerAddress.Substring(x, 2), 16))
                          .ToArray();
                return register;
            }
            catch (Exception ex)
            {
                throw new RegisterConversionException($"Unknown error in Hex conversion {registerAddress}", ex);
            }
        }

        public static int ConvertIpToNumber(string dottedIpAddress)
        {
            int num = 0;
            if (dottedIpAddress == "")
            {
                return 0;
            }
            else
            {
                var splitIpAddress = dottedIpAddress.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                int i;
                for (i = splitIpAddress.Length - 1; i >= 0; i--)
                {
                    num += ((int.Parse(splitIpAddress[i]) % 256) * (int)Math.Pow(256, (3 - i)));
                }
                return num;
            }
        }
    }
}