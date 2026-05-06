namespace GigeVision.Core.Services
{
    /// <summary>
    /// Describes a vendor-specific pixel format that is not part of the standard PFNC table.
    /// Register instances with <see cref="PixelFormatRegistry"/>.
    /// </summary>
    public sealed class CustomPixelFormatInfo
    {
        /// <summary>Human-readable name, e.g. "QOI_BayerRG8".</summary>
        public string Name { get; }

        /// <summary>
        /// Effective bits consumed per pixel for buffer-size calculation.
        /// For losslessly-compressed formats transmit as a worst-case uncompressed size
        /// (e.g. 8 for QOI_BayerRG8).
        /// </summary>
        public int EffectiveBitsPerPixel { get; }

        /// <summary>
        /// True when the format carries a Bayer colour-filter-array pattern and the
        /// library should allocate a 3-channel (RGB) debayer output buffer.
        /// </summary>
        public bool IsBayer { get; }

        public CustomPixelFormatInfo(string name, int effectiveBitsPerPixel, bool isBayer)
        {
            Name = name;
            EffectiveBitsPerPixel = effectiveBitsPerPixel;
            IsBayer = isBayer;
        }
    }
}
