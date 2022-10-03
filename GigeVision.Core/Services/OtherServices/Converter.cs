using GigeVision.Core.Exceptions;
using System;
using System.Linq;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// General converter
    /// </summary>
    public static class Converter
    {
        /// <summary>
        /// Converts String to byte array
        /// </summary>
        /// <param name="registerAddress">Register address that needs to be converted</param>
        /// <returns>Hex bytes</returns>
        public static byte[] RegisterStringToByteArray(string registerAddress)
        {
            registerAddress = registerAddress.Replace("0x", "");
            if (registerAddress.Length > 8)
            {
                throw new RegisterConversionException($"Length Miss match {registerAddress}");
            }

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
                byte[] register = Enumerable.Range(0, registerAddress.Length)
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

        /// <summary>
        /// Converts IP to int
        /// </summary>
        /// <param name="dottedIpAddress">String IP that needs to be converted</param>
        /// <returns>IP in int</returns>
        public static uint IpToNumber(string dottedIpAddress)
        {
            uint num = 0;
            if (dottedIpAddress?.Length == 0)
            {
                return 0;
            }
            else
            {
                string[] splitIpAddress = dottedIpAddress.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                int i;
                for (i = splitIpAddress.Length - 1; i >= 0; i--)
                {
                    num += ((uint.Parse(splitIpAddress[i]) % 256) * (uint)Math.Pow(256, 3 - i));
                }
                return num;
            }
        }

        /// <summary>
        /// Converts String to byte array
        /// </summary>
        /// <param name="registerAddresses">Register address that needs to be converted</param>
        /// <returns>Hex bytes</returns>
        public static byte[] RegisterStringsToByteArray(string[] registerAddresses)
        {
            byte[] registersBytes = new byte[4 * registerAddresses.Length];
            int registerIndex = 0;
            foreach (string register in registerAddresses)
            {
                byte[] registerBytes = RegisterStringToByteArray(register);
                registersBytes[0 + (registerIndex * 4)] = registerBytes[0];
                registersBytes[1 + (registerIndex * 4)] = registerBytes[1];
                registersBytes[2 + (registerIndex * 4)] = registerBytes[2];
                registersBytes[3 + (registerIndex * 4)] = registerBytes[3];
                registerIndex++;
            }
            return registersBytes;
        }

        /// <summary>
        /// Hex to string byte array
        /// </summary>
        /// <param name="hexString">General hex string</param>
        /// <returns>Hex bytes</returns>
        public static byte[] HexStringToByteArray(string hexString)
        {
            hexString = hexString.Replace("0x", "");
            if (hexString.Length % 2 == 1)
            {
                hexString = "0" + hexString;
            }
            try
            {
                byte[] register = Enumerable.Range(0, hexString.Length)
                          .Where(x => x % 2 == 0)
                          .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                          .ToArray();
                return register;
            }
            catch (Exception ex)
            {
                throw new RegisterConversionException($"Unknown error in Hex conversion {hexString}", ex);
            }
        }

        /// <summary>
        /// Converts IP to int
        /// </summary>
        /// <param name="dottedIpAddress">String ip that needs to be converted</param>
        /// <returns>IP in int</returns>
        public static int ConvertIpToNumber(string dottedIpAddress)
        {
            int num = 0;
            if (string.IsNullOrEmpty(dottedIpAddress))
            {
                return 0;
            }
            else
            {
                string[] splitIpAddress = dottedIpAddress.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
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