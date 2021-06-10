namespace GigeVision.Core.Models
{
    public class GvspInfo
    {
        public int PayloadOffset;
        public int PacketIDIndex;
        public int DataIdentifier;
        public int DataEndIdentifier;
        public int PacketLength;
        public int PayloadSize;
        public bool IsValid;
        public int FinalPacketID;
    }
}