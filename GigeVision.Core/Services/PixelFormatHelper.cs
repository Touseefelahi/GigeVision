using System;
using GigeVision.Core.Enums;

namespace GigeVision.Core.Services
{
    public static class PixelFormatHelper
    {
        /// <summary>
        /// Returns the human-readable name for a pixel format value.
        /// Checks <see cref="PixelFormatRegistry"/> first, then the standard
        /// <see cref="GigeVision.Core.Enums.PixelFormat"/> enum, then falls back to hex.
        /// </summary>
        public static string GetName(uint pixelFormat)
        {
            if (PixelFormatRegistry.TryGet(pixelFormat, out var info))
                return info.Name;

            if (Enum.IsDefined(typeof(PixelFormat), pixelFormat))
                return ((PixelFormat)pixelFormat).ToString();

            return $"0x{pixelFormat:X8}";
        }

        public static int GetBytesPerPixelRoundedUp(uint pixelFormat)
        {
            return DivideRoundUp(GetEffectiveBitsPerPixel(pixelFormat), 8);
        }

        public static int GetFrameSize(int width, int height, uint pixelFormat)
        {
            if (width <= 0 || height <= 0)
            {
                return 0;
            }

            long totalBits = (long)width * height * GetEffectiveBitsPerPixel(pixelFormat);
            return (int)DivideRoundUp(totalBits, 8);
        }

        public static int GetLineSize(int width, uint pixelFormat)
        {
            if (width <= 0)
            {
                return 0;
            }

            long totalBits = (long)width * GetEffectiveBitsPerPixel(pixelFormat);
            return (int)DivideRoundUp(totalBits, 8);
        }

        /// <summary>
        /// Returns true when the pixel format carries a Bayer CFA pattern.
        /// Checks the <see cref="PixelFormatRegistry"/> first so that vendor-specific
        /// Bayer formats (e.g. Lucid QOI_BayerRG8) are recognised correctly.
        /// Falls back to testing whether the standard PFNC enum name contains "Bayer".
        /// </summary>
        public static bool IsBayerFormat(uint pixelFormat)
        {
            if (PixelFormatRegistry.TryGet(pixelFormat, out var info))
            {
                return info.IsBayer;
            }

            if (Enum.IsDefined(typeof(PixelFormat), pixelFormat))
            {
                return ((PixelFormat)pixelFormat).ToString().Contains("Bayer");
            }

            return false;
        }

        private static int GetEffectiveBitsPerPixel(uint pixelFormat)
        {
            // Registry entries take precedence — useful for compressed formats where
            // the PFNC bits-per-pixel field does not reflect worst-case buffer requirements.
            if (PixelFormatRegistry.TryGet(pixelFormat, out var info))
            {
                return info.EffectiveBitsPerPixel > 0 ? info.EffectiveBitsPerPixel : 8;
            }

            int effectiveBits = (int)((pixelFormat >> 16) & 0xFF);
            return effectiveBits > 0 ? effectiveBits : 8;
        }

        private static int DivideRoundUp(long value, int divisor)
        {
            return (int)((value + divisor - 1) / divisor);
        }
    }
}