using System;
using System.Collections.Generic;
using System.Text;

namespace GenICam
{
    /// <summary>
    /// General reply: can be used for UDP/TCP
    /// </summary>
    public interface IReplyPacket
    {
        /// <summary>
        /// Error to display if any
        /// </summary>
        string Error { get; set; }

        /// <summary>
        /// IP address of the sender
        /// </summary>
        string IPSender { get; set; }

        /// <summary>
        /// Command sent
        /// </summary>
        bool IsSent { get; set; }

        /// <summary>
        /// Command Sent and camera replied
        /// </summary>
        bool IsSentAndReplyReceived { get; set; }

        /// <summary>
        /// Sending Port
        /// </summary>
        int PortSender { get; set; }

        /// <summary>
        /// Raw reply packet
        /// </summary>
        List<byte> Reply { get; }

        /// <summary>
        /// Register value
        /// </summary>
        public uint RegisterValue { get; set; }

        /// <summary>
        /// Memory Value
        /// </summary>
        public byte[] MemoryValue { get; set; }

        /// <summary>
        /// It sets the list of byte
        /// </summary>
        /// <param name="reply"></param>
        void SetReply(byte[] reply);

        /// <summary>
        /// It sets the list of byte
        /// </summary>
        /// <param name="reply"></param>
        void SetReply(List<byte> reply);
    }
}