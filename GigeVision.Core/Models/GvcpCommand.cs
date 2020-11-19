using GigeVision.Core.Enums;
using System;
using System.Collections.Generic;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Generates GVCP command
    /// </summary>
    public class GvcpCommand
    {
        /// <summary>
        /// GVCP header
        /// </summary>
        public readonly byte GvcpHeader = 0x42;

        /// <summary>
        /// Flag for acknowledgement
        /// </summary>
        public readonly byte flag = 0x01;

        /// <summary>
        /// Generates Gvcp Command
        /// </summary>
        /// <param name="adress"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="requestID"></param>
        public GvcpCommand(byte[] adress, GvcpCommandType type, uint value = 0, ushort requestID = 0, ushort count = 0)
        {
            GenerateCommand(adress, type, requestID, value, count);
        }

        /// <summary>
        /// Geneartes GVCP Command
        /// </summary>
        /// <param name="address"></param>
        /// <param name="valuesToWrite"></param>
        /// <param name="requestID"></param>
        public GvcpCommand(string[] address, uint[] valuesToWrite, ushort requestID = 0, ushort count = 0)
        {
            GenerateCommand(address, valuesToWrite: valuesToWrite, requestID, count);
        }

        /// <summary>
        /// Generate read command
        /// </summary>
        /// <param name="address"></param>
        /// <param name="requestID"></param>
        public GvcpCommand(string[] address, ushort requestID = 0)
        {
            GenerateCommand(address, requestID);
        }

        /// <summary>
        /// Generate GVCP command
        /// </summary>
        /// <param name="command"></param>
        public GvcpCommand(GvcpCommandType command)
        {
            GenerateCommand(null, command, 0);
        }

        /// <summary>
        /// GVCP command type
        /// </summary>
        public GvcpCommandType Type { get; set; }

        /// <summary>
        /// Command bytes for generated command
        /// </summary>
        public byte[] CommandBytes { get; private set; }

        /// <summary>
        /// Length of GVCP command
        /// </summary>
        public int Length { get => CommandBytes.Length; }

        /// <summary>
        /// Request ID
        /// </summary>
        public ushort RequestId { get; set; }

        /// <summary>
        /// Is command valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Value of register (for read command)
        /// </summary>
        public uint Value { get; set; }

        /// <summary>
        /// Values for register (for multiple read commands)
        /// </summary>
        public List<uint> Values { get; set; }

        /// <summary>
        /// Address of register
        /// </summary>
        public byte[] Address { get; set; }

        /// <summary>
        /// Generate general GVCP command
        /// </summary>
        /// <param name="adress"></param>
        /// <param name="type"></param>
        /// <param name="requestID"></param>
        /// <param name="valueToWrite"></param>
        public void GenerateCommand(byte[] adress, GvcpCommandType type, ushort requestID, uint valueToWrite = 0, ushort count = 0)
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

                case GvcpCommandType.ReadReg:
                    GenerateReadRegisterCommand(requestID);
                    break;

                case GvcpCommandType.WriteReg:
                    GenerateWriteRegisterCommand(requestID, valueToWrite);
                    break;

                case GvcpCommandType.ReadMem:
                    GenerateReadMemoryCommand(requestID, count);
                    break;

                case GvcpCommandType.WriteMem:
                    GenerateWriteMemoryCommand(requestID, valueToWrite);
                    break;
            }
        }

        private void GenerateCommand(string[] addressess, ushort requestID, ushort count = 0)
        {
            var commandHeader = GenerateCommandHeader(GvcpCommandType.ReadReg, addressess.Length, requestID);
            CommandBytes = new byte[8 + (addressess.Length * 4)];
            Array.Copy(commandHeader, 0, CommandBytes, 0, commandHeader.Length);
            var registerBytes = Converter.RegisterStringsToByteArray(addressess);
            Array.Copy(registerBytes, 0, CommandBytes, commandHeader.Length, registerBytes.Length);
        }

        private void GenerateCommand(string[] addressess, uint[] valuesToWrite, ushort requestID, ushort count = 0)
        {
            if (addressess.Length != valuesToWrite.Length) throw new Exception("Length missmatch between address and values to write");
            var commandHeader = GenerateCommandHeader(GvcpCommandType.WriteReg, addressess.Length, requestID);

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

        private void GenerateReadRegisterCommand(ushort requestID)
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

        private void GenerateWriteRegisterCommand(ushort requestID, uint valueToWrite)
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

        private void GenerateReadMemoryCommand(ushort requestID, ushort count)
        {
            //byte[] readFileHeader = { 0x42, 0x01, 0x00, 0x84, 0x00, 0x08,
            //                    requestID[0], requestID[1],
            //                    tempFileAddress[0], tempFileAddress[1], tempFileAddress[2], tempFileAddress[3],
            //                    count[0], count[1], count[2], count[3] };
            //client.Send(readFileHeader, readFileHeader.Length);

            var packetLength = (short)(Address.Length) + 4;
            var countBytes = BitConverter.GetBytes(count);
            var bytes = new byte[]
            {
                        GvcpHeader,
                        flag,
                        (byte)(((short)Type & 0xFF00) >> 8), (byte)Type,
                        (byte)((packetLength & 0xFF00) >> 8), (byte)packetLength,
                        (byte)((requestID & 0xFF00) >> 8), (byte)requestID,
            };
            Array.Reverse(countBytes);
            CommandBytes = new byte[bytes.Length + Address.Length + 2 + countBytes.Length];
            bytes.CopyTo(CommandBytes, 0);
            Address.CopyTo(CommandBytes, bytes.Length);
            countBytes.CopyTo(CommandBytes, (bytes.Length + Address.Length + 2));
        }

        private void GenerateWriteMemoryCommand(ushort requestID, uint valueToWrite)
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

            ushort packetLength = (ushort)(4 * (type == GvcpCommandType.WriteReg ? 2 : 1) * valuesToReadOrWrite);
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