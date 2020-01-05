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

        public GvcpCommand(string[] address, uint[] valuesToWrite, ushort requestID = 0)
        {
            GenerateCommand(address, valuesToWrite: valuesToWrite, requestID);
        }

        public GvcpCommand(string[] address, ushort requestID = 0)
        {
            GenerateCommand(address, requestID);
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

        public void GenerateCommand(byte[] adress, GvcpCommandType type, ushort requestID, uint valueToWrite = 0)
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
                    GenerateWriteCommand(requestID, valueToWrite);
                    break;
            }
        }

        private void GenerateCommand(string[] addressess, ushort requestID)
        {
            var commandHeader = GenerateCommandHeader(GvcpCommandType.Read, addressess.Length, requestID);
            CommandBytes = new byte[8 + (addressess.Length * 4)];
            Array.Copy(commandHeader, 0, CommandBytes, 0, commandHeader.Length);
            var registerBytes = Converter.RegisterStringsToByteArray(addressess);
            Array.Copy(registerBytes, 0, CommandBytes, commandHeader.Length, registerBytes.Length);
        }

        private void GenerateCommand(string[] addressess, uint[] valuesToWrite, ushort requestID)
        {
            if (addressess.Length != valuesToWrite.Length) throw new Exception("Length missmatch between address and values to write");
            var commandHeader = GenerateCommandHeader(GvcpCommandType.Write, addressess.Length, requestID);

            int index = 0;
            CommandBytes = new byte[8 + (addressess.Length * 4 * 2)];
            Array.Copy(commandHeader, 0, CommandBytes, 0, commandHeader.Length);
            foreach (var address in addressess)
            {
                var addressBytes = Converter.RegisterStringToByteArray(address);
                var bytesValue = BitConverter.GetBytes(valuesToWrite[index]);
                Array.Reverse(bytesValue);

                Array.Copy(addressBytes, 0, CommandBytes, 8 + (8 * index), 4);
                Array.Copy(bytesValue, 0, CommandBytes, 12 + (8 * index), 4);
                index++;
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

        private void GenerateWriteCommand(ushort requestID, uint valueToWrite)
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

            var bytes = BitConverter.GetBytes(valueToWrite);
            Array.Reverse(bytes);
            bytes.CopyTo(CommandBytes, CommandBytes.Length - 4);
        }

        private byte[] GenerateCommandHeader(GvcpCommandType type, int valuesToReadOrWrite = 1, uint requestID = 0)
        {
            Random random = new Random();
            Type = type;
            if (requestID == 0)
            {
                requestID = (ushort)random.Next(1, 60000);
            }

            ushort packetLength = (ushort)(8 + (4 * (type == GvcpCommandType.Write ? 2 : 1)));
            return new byte[]
                        {
                          GvcpHeader,
                          flag,
                          (byte)(((short)Type & 0xFF00) >> 8), (byte)Type,
                          (byte)((packetLength & 0xFF00) >> 8), (byte)packetLength,
                          (byte)((requestID & 0xFF00) >> 8), (byte)requestID,
                        };
        }
    }
}