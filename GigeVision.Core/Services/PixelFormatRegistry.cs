using System.Collections.Concurrent;

namespace GigeVision.Core.Services
{
    /// <summary>
    /// Application-level registry for vendor-specific pixel formats that are not defined in
    /// the standard PFNC/GigE Vision pixel-format table.
    /// <para>
    /// Register a custom format once at startup before opening any camera stream:
    /// <code>
    /// // Lucid QOI-compressed Bayer RG 8-bit
    /// PixelFormatRegistry.Register(0x0108_0100, new CustomPixelFormatInfo("QOI_BayerRG8", 8, isBayer: true));
    /// </code>
    /// The registered effective-bits-per-pixel value is used for receive-buffer sizing.
    /// The isBayer flag drives the 3-channel debayer output buffer when IsRawFrame is false.
    /// </para>
    /// </summary>
    public static class PixelFormatRegistry
    {
        private static readonly ConcurrentDictionary<uint, CustomPixelFormatInfo> _formats =
            new ConcurrentDictionary<uint, CustomPixelFormatInfo>();

        /// <summary>
        /// Registers a vendor-specific pixel format.
        /// Overwrites any previous registration for the same <paramref name="value"/>.
        /// </summary>
        /// <param name="value">32-bit PFNC-style pixel-format code reported by the camera.</param>
        /// <param name="info">Metadata describing the format.</param>
        public static void Register(uint value, CustomPixelFormatInfo info)
        {
            _formats[value] = info;
        }

        /// <summary>
        /// Attempts to look up a previously registered custom format.
        /// </summary>
        public static bool TryGet(uint value, out CustomPixelFormatInfo info)
        {
            return _formats.TryGetValue(value, out info);
        }
    }
}
