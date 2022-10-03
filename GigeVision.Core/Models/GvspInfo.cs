using System;
using System.Dynamic;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// GVSP info
    /// </summary>
    public class GvspInfo
    {
        /// <summary>
        /// Frame counter index start
        /// </summary>
        public int BlockIDIndex { get; set; }

        /// <summary>
        /// 2 or 8
        /// </summary>
        public int BlockIDLength { get; set; }

        /// <summary>
        /// Bytes Per Pixel
        /// </summary>
        public int BytesPerPixel { get; set; }

        /// <summary>
        /// Data end identifier 0x04 for old version and 0x84 for new
        /// </summary>
        public int DataEndIdentifier { get; set; }

        /// <summary>
        /// Data identifier 0x02 for old version and 0x82 for new
        /// </summary>
        public int DataIdentifier { get; set; }

        /// <summary>
        /// Final Packet ID
        /// </summary>
        public int FinalPacketID { get; set; }

        /// <summary>
        /// Image Height
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// If software read the GVSP stream as version 2
        /// </summary>
        public bool IsDecodingAsVersion2 { get; set; }

        /// <summary>
        /// It is supposed to be Payload type enum instead of a bool
        /// </summary>
        public bool IsImageData { get; set; }

        /// <summary>
        /// Offset X
        /// </summary>
        public int OffsetX { get; set; }

        /// <summary>
        /// Offset Y
        /// </summary>
        public int OffsetY { get; set; }

        /// <summary>
        /// Packet ID index in the GVSP header
        /// </summary>
        public int PacketIDIndex { get; set; }

        /// <summary>
        /// Normal Packet Length, Payload with Header
        /// </summary>
        public int PacketLength { get; set; }

        /// <summary>
        /// Payload offset for GVSP Header length
        /// </summary>
        public int PayloadOffset { get; set; }

        /// <summary>
        /// Payload size only, without header
        /// </summary>
        public int PayloadSize { get; set; }

        /// <summary>
        /// Total bytes in raw image
        /// </summary>
        public int RawImageSize { get; set; }

        /// <summary>
        /// Timestamp index start
        /// </summary>
        public int TimeStampIndex { get; set; }

        /// <summary>
        /// Image width
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// It will set the parameters based on the already set Decoding type <see cref="IsDecodingAsVersion2"/>
        /// </summary>
        public void SetDecodingTypeParameter()
        {
            BlockIDIndex = IsDecodingAsVersion2 ? 8 : 2;
            BlockIDLength = IsDecodingAsVersion2 ? 8 : 2;
            PacketIDIndex = IsDecodingAsVersion2 ? 18 : 6;
            PayloadOffset = IsDecodingAsVersion2 ? 20 : 8;
            TimeStampIndex = IsDecodingAsVersion2 ? 24 : 12;
            DataIdentifier = IsDecodingAsVersion2 ? 0x83 : 0x03;
            DataEndIdentifier = IsDecodingAsVersion2 ? 0x82 : 0x02;
        }
    }
}