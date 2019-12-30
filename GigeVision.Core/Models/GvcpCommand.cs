using GigeVision.Core.Enums;
using System;
using System.Collections.Generic;

namespace GigeVision.Core.Models
{
    public class GvcpCommand
    {
        public readonly byte GvcpHeader = 0x42;
        public readonly byte flag = 0x01;

        public GvcpCommand(byte[] adress, GvcpCommandType type, uint value = 0, ushort requestID = 0)
        {
            GenerateCommand(adress, type, requestID, value);
        }

        public GvcpCommand(byte[] adress, GvcpCommandType type, uint[] valuesToWrite, ushort requestID = 0)
        {
            GenerateCommand(adress, type, requestID, valuesToWrite: valuesToWrite);
        }

        public GvcpCommand(GvcpCommandType command)
        {
            GenerateCommand(null, command, 0);
        }

        public GvcpCommandType Type { get; set; }

        public byte[] CommandBytes { get; private set; }

        public int Length { get => CommandBytes.Length; }

        public ushort RequestId { get; set; }

        public bool IsValid { get; set; }

        public uint Value { get; set; }

        public List<uint> Values { get; set; }

        public byte[] Address { get; set; }

        public void GenerateCommand(byte[] adress, GvcpCommandType type, ushort requestID, uint valueToWrite = 0, uint[] valuesToWrite = null)
        {
            Random random = new Random();
            Address = adress;
            Type = type;
            if (requestID == 0)
            {
                requestID = (ushort)random.Next(1, 65535);
            }
            RequestId = requestID;
            switch (Type)
            {
                case GvcpCommandType.Discovery:
                    CommandBytes = new byte[]
                    {
                        GvcpHeader,
                        0x11, //For broad cast devices flag must be 0x11
                        (byte)(((short)Type & 0xFF00) >> 8), (byte)Type,
                         0x00, 0x00,
                        (byte)((requestID & 0xFF00) >> 8), (byte)requestID,
                    };
                    break;

                case GvcpCommandType.Read:
                    GenerateReadCommand(requestID);
                    break;

                case GvcpCommandType.Write:
                    GenerateWriteCommand(requestID, valueToWrite, valuesToWrite);
                    break;
            }
        }

        private void GenerateReadCommand(ushort requestID)
        {
            var packetLength = (short)(Address.Length);
            var bytes = new byte[]
            {
                        GvcpHeader,
                        flag,
                        (byte)(((short)Type & 0xFF00) >> 8), (byte)Type,
                        (byte)((packetLength & 0xFF00) >> 8), (byte)packetLength,
                        (byte)((requestID & 0xFF00) >> 8), (byte)requestID,
            };
            CommandBytes = new byte[bytes.Length + Address.Length];
            bytes.CopyTo(CommandBytes, 0);
            Address.CopyTo(CommandBytes, bytes.Length);
        }

        private void GenerateWriteCommand(ushort requestID, uint valueToWrite, uint[] valuesToWrite)
        {
            ushort packetLength = (ushort)(Address.Length * 2);
            var bytes2 = new byte[]
                        {
                          GvcpHeader,
                          flag,
                          (byte)(((short)Type & 0xFF00) >> 8), (byte)Type,
                          (byte)((packetLength & 0xFF00) >> 8), (byte)packetLength,
                          (byte)((requestID & 0xFF00) >> 8), (byte)requestID,
                        };
            CommandBytes = new byte[bytes2.Length + (Address.Length * 2)];
            bytes2.CopyTo(CommandBytes, 0);
            Address.CopyTo(CommandBytes, bytes2.Length);
            if (valuesToWrite == null) //Use Single values
            {
                var bytes = BitConverter.GetBytes(valueToWrite);
                Array.Reverse(bytes);
                bytes.CopyTo(CommandBytes, CommandBytes.Length - 4);
            }
            else //Use multiple values
            {
                var firstValueAddress = 8 + ((CommandBytes.Length - 8) / 2);
                var countIndex = 0;
                foreach (var singleValue in valuesToWrite)
                {
                    var bytes = BitConverter.GetBytes(singleValue);
                    Array.Reverse(bytes);
                    bytes.CopyTo(CommandBytes, firstValueAddress + (countIndex * 4));
                    countIndex++;
                }
            }
        }
    }
}