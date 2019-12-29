using GigeVision.Core.Enums;
using System;
using System.Collections.Generic;

namespace GigeVision.Core.Models
{
    public class GvcpReply
    {
        public GvcpReply(byte[] buffer)
        {
            DetectCommand(buffer);
        }

        public GvcpReply()
        {
        }

        /// <summary>
        /// Error to display if any
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// IP address of the sender
        /// </summary>
        public string IPSender { get; set; }

        /// <summary>
        /// Command sent
        /// </summary>
        public bool IsSent { get; set; }

        /// <summary>
        /// Command Sent and camera replied
        /// </summary>
        public bool IsSentAndReplyReceived { get; set; }

        /// <summary>
        /// Sending Port
        /// </summary>
        public int PortSender { get; set; }

        /// <summary>
        /// Raw reply packet
        /// </summary>
        public List<byte> Reply { get; private set; }

        /// <summary>
        /// Register value
        /// </summary>
        public uint RegisterValue { get; set; }

        /// <summary>
        /// For Multiple register reading
        /// </summary>
        public List<uint> RegisterValues { get; set; }

        public ushort AcknowledgementID { get; set; }

        public bool IsValid { get; set; }

        public GvcpCommandType Type { get; set; }

        public GvcpStatus Status { get; set; }

        public void SetReply(byte[] reply)
        {
            Reply = new List<byte>(reply);
        }

        public void SetReply(List<byte> reply)
        {
            Reply = reply;
        }

        public void DetectCommand(byte[] buffer)
        {
            if (buffer?.Length > 7)
            {
                Status = (GvcpStatus)((buffer[0] << 8) | (buffer[1]));
                IsSentAndReplyReceived = true;
                GvcpCommandType commandType = (GvcpCommandType)((buffer[2] << 8) | (buffer[3]));
                if (Enum.IsDefined(typeof(GvcpCommandType), commandType))
                {
                    IsValid = true;
                    Type = commandType;
                    SetReply(buffer);
                }
                else
                {
                    IsValid = false;
                    return;
                }

                switch (Type)
                {
                    case GvcpCommandType.Discovery:
                        break;

                    case GvcpCommandType.Read:
                        break;

                    case GvcpCommandType.ReadAck:
                        if (buffer?.Length < 13)//Single Register reply
                        {
                            RegisterValue = (uint)((buffer[8] << 24) | (buffer[9] << 16) | (buffer[10] << 8) | (buffer[11]));
                        }
                        else //Multiple register reply
                        {
                            int totalRegisters = (buffer.Length - 8) / 4;
                            RegisterValues = new List<uint>();
                            for (int i = 0; i < totalRegisters; i++)
                            {
                                RegisterValues.Add((uint)(
                                    (buffer[08 + (4 * i)] << 24) |
                                    (buffer[09 + (4 * i)] << 16) |
                                    (buffer[10 + (4 * i)] << 8) |
                                    (buffer[11 + (4 * i)])));
                            }
                        }
                        break;

                    case GvcpCommandType.Write:
                        break;

                    case GvcpCommandType.WriteAck:
                        break;

                    case GvcpCommandType.Invalid:
                        break;
                }
                AcknowledgementID = (ushort)((buffer[6] << 8) | (buffer[7]));
            }
        }
    }
}