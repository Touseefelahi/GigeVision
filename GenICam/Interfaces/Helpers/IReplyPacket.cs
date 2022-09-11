using System.Collections.Generic;

namespace GenICam
{
    /// <summary>
    /// General reply: can be used for UDP/TCP.
    /// </summary>
    public interface IReplyPacket
    {
        /// <summary>
        /// Gets or sets error to display if any.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets iP address of the sender.
        /// </summary>
        public string IPSender { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether command sent.
        /// </summary>
        public bool IsSent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether command Sent and camera replied.
        /// </summary>
        public bool IsSentAndReplyReceived { get; set; }

        /// <summary>
        /// Gets or sets sending Port.
        /// </summary>
        public int PortSender { get; set; }

        /// <summary>
        /// Gets raw reply packet.
        /// </summary>
        public List<byte> Reply { get; }

        /// <summary>
        /// Gets or sets register value.
        /// </summary>
        public uint RegisterValue { get; set; }

        /// <summary>
        /// Gets or sets memory Value.
        /// </summary>
        public byte[] MemoryValue { get; set; }

        /// <summary>
        /// It sets the list of byte.
        /// </summary>
        /// <param name="reply">The reply bytes.</param>
        public void SetReply(byte[] reply);

        /// <summary>
        /// It sets the list of byte.
        /// </summary>
        /// <param name="reply">The reply bytes.</param>
        public void SetReply(List<byte> reply);
    }
}