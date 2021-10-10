namespace GigeVision.Core.Models
{
    /// <summary>
    /// GVSP info
    /// </summary>
    public class GvspInfo
    {
        /// <summary>
        /// Payload offset for GVSP Header length
        /// </summary>
        public int PayloadOffset { get; set; }

        /// <summary>
        /// Packet ID index in the GVSP header
        /// </summary>
        public int PacketIDIndex { get; set; }

        /// <summary>
        /// Data identifier 0x02 for old version and 0x82 for new
        /// </summary>
        public int DataIdentifier { get; set; }

        /// <summary>
        /// Data end identifier 0x04 for old version and 0x84 for new
        /// </summary>
        public int DataEndIdentifier { get; set; }

        /// <summary>
        /// Normal Packet Length, Payload with Header
        /// </summary>
        public int PacketLength { get; set; }

        /// <summary>
        /// Payload size only, without header
        /// </summary>
        public int PayloadSize { get; set; }

        /// <summary>
        /// Final Packet ID
        /// </summary>
        public int FinalPacketID { get; set; }

        /// <summary>
        /// Frame counter index start
        /// </summary>
        public int BlockIDIndex { get; set; }

        /// <summary>
        /// 2 or 8
        /// </summary>
        public int BlockIDLength { get; set; }

        /// <summary>
        /// Timestamp index start
        /// </summary>
        public int TimeStampIndex { get; set; }
    }
}