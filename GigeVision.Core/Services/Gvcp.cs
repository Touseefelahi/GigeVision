﻿using GenICam;
using GigeVision.Core.Enums;
using GigeVision.Core.Exceptions;
using GigeVision.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using GigeVision.Core.Models;
using Converter = GigeVision.Core.Models.Converter;

namespace GigeVision.Core.Services
{
    /// <summary>
    /// Max GVCP payload = 540 bytes and must be multiple of 32
    /// </summary>
    public class Gvcp : IGvcp
    {
        private List<CameraInformation> cameraInfoList;
        private string cameraIP = "";

        private ushort gvcpRequestID = 1;

        private bool isHeartBeatThreadRunning;

        private XmlHelper xmlHelper;

        /// <summary>
        /// Gvcp constructor, initializes camera IP, and try to get register values
        /// </summary>
        /// <param name="ip"></param>
        public Gvcp(string ip)
        {
            CameraIp = ip;
        }
        
        /// <summary>
        /// Gvcp constructor, initializes camera IP and socket timeout, and try to get register values
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="socketReadTimeoutInMilliseconds"></param>
        public Gvcp(string ip, int socketReadTimeoutInMilliseconds)
        {
            CameraIp = ip;
            ReceiveTimeoutInMilliseconds = socketReadTimeoutInMilliseconds;
        }

        /// <summary>
        /// Default GVCP constructor
        /// </summary>
        public Gvcp()
        {
            ReceiveTimeoutInMilliseconds = 1000;
        }
        
        /// <summary>
        /// The socket read timeout in milliseconds. Set -1 for infinite timeout
        /// </summary>
        public int ReceiveTimeoutInMilliseconds { get; set; }

        /// <summary>
        /// Camera IP, whenever changed, library tries to get latest register values
        /// </summary>
        public string CameraIp
        {
            get => cameraIP;
            set
            {
                if (ValidateIp(value))
                {
                    if (value != cameraIP)
                    {
                        cameraIP = value;
                        Reconnect();
                        CameraIpChanged?.Invoke(null, null);
                    }
                }
            }
        }

        /// <summary>
        /// Event fired whenever camera IP changed: used to get registers
        /// </summary>
        public EventHandler CameraIpChanged { get; set; }

        public List<ICategory> CategoryDictionary { get; private set; }

        /// <summary>
        /// Control socket
        /// </summary>
        public UdpClient ControlSocket { get; private set; }

        /// <summary>
        /// It can be for any thing, to update fps to check devices
        /// </summary>
        public EventHandler ElapsedOneSecond { get; set; }

        public bool IsKeepingAlive { get; set; }

        public bool IsLoadingXml { get; private set; }

        public bool IsXmlFileLoaded { get; private set; }

        /// <summary>
        /// Controlling port of GVCP
        /// </summary>
        public int PortControl { get; set; }

        /// <summary>
        /// GVCP Port
        /// </summary>
        public int PortGvcp { get => 3956; }

        /// <summary>
        /// If true, heartbeat command will be sent to the devices after regular interval
        /// </summary>

        #region Status Commands

        public Dictionary<string, (IPValue, IRegister)> RegistersDictionary { get; set; }

        /// <summary>
        /// Check camera status
        /// </summary>
        /// <param name="ip">IP Camera</param>
        /// <returns>Camera Status: Available/InControl or Unavailable</returns>
        public async Task<CameraStatus> CheckCameraStatusAsync(string ip)
        {
            if (ValidateIp(ip))
            {
                var cameraStatusPacket = await ReadRegisterAsync(ip, GvcpRegister.GevCCP).ConfigureAwait(false);
                return cameraStatusPacket.RegisterValue switch
                {
                    0 => CameraStatus.Available,
                    2 => CameraStatus.InControl,
                    _ => CameraStatus.UnAvailable,
                };
            }
            else
            {
                return CameraStatus.UnAvailable;
            }
        }

        /// <summary>
        /// Check camera status
        /// </summary>
        /// <returns>Camera Status: Available/InControl or Unavailable</returns>
        public async Task<CameraStatus> CheckCameraStatusAsync()
        {
            return await CheckCameraStatusAsync(CameraIp).ConfigureAwait(false);
        }

        /// <summary>
        /// Forces the IP of camera to be changed to the given IP
        /// </summary>
        /// <param name="macAddress">MAC address of the camera</param>
        /// <param name="iPToSet">IP of camera that needs to be set</param>
        /// <returns>Success Status</returns>
        public async Task<bool> ForceIPAsync(byte[] macAddress, string iPToSet, string netmask)
        {
            var forceIpCommand = new byte[64];

            var forceIPCommandHeader = new byte[] { 0x42, 0x01, 0x00, 0x04 };
            Array.Copy(forceIPCommandHeader, 0, forceIpCommand, 0, forceIPCommandHeader.Length);//4bytes, TotalLength=4

            var packetSize = BitConverter.GetBytes((short)(56));
            Array.Reverse(packetSize, 0, 2);
            Array.Copy(packetSize, 0, forceIpCommand, 4, packetSize.Length);//2bytes, TotalLength=6

            int packetId = 12;
            var packetIdBytes = BitConverter.GetBytes((short)(packetId));
            Array.Reverse(packetIdBytes, 0, 2);
            Array.Copy(packetIdBytes, 0, forceIpCommand, 6, packetIdBytes.Length);//2bytes, TotalLength=8

            Array.Copy(macAddress, 0, forceIpCommand, 10, macAddress.Length);//6bytes, TotalLength = (2 Reserved bytes) + (6 bytes Mac Address) + (12 reserved bytes) = 28
            var ipBytes = BitConverter.GetBytes(Converter.ConvertIpToNumber(iPToSet));
            Array.Reverse(ipBytes, 0, 4);
            Array.Copy(ipBytes, 0, forceIpCommand, 28, 4);//4bytes, TotalLength= 32 + (12 reserved bytes) = 44

            var maskBytes = BitConverter.GetBytes(Converter.ConvertIpToNumber(netmask));
            Array.Reverse(maskBytes, 0, 4);
            Array.Copy(maskBytes, 0, forceIpCommand, 44, 4);//4bytes, TotalLength= 48 + (12 reserved bytes) = 60

            var gateWayBytes = new byte[4];
            Array.Copy(ipBytes, 0, gateWayBytes, 0, 4);
            gateWayBytes[3] = 0x01;
            Array.Copy(gateWayBytes, 0, forceIpCommand, 60, 4);//4bytes, TotalLength= 64

            var broadCast = SendBroadCastPacket(forceIpCommand, ParseReply);
            bool status = false;
            void ParseReply(IPEndPoint endPoint, byte[] data)
            {
                if (data?.Length > 5)
                {
                    if (data[3] == 0x05 && data[0] == 0 && data[1] == 0) //ForceIp acknowledgment
                    {
                        status = true;
                    }
                }
            }
            await broadCast;
            return status;
        }

        /// <summary>
        /// Forces the IP to the camera
        /// </summary>
        /// <param name="macAddress">Mac Address of Camera</param>
        /// <param name="iPToSet">IP to set</param>
        /// <returns></returns>
        public async Task<bool> ForceIPAsync(string macAddress, string iPToSet, string netmask)
        {
            return await ForceIPAsync(Converter.HexStringToByteArray(macAddress), iPToSet, netmask).ConfigureAwait(false);
        }

        /// <summary>
        /// It will get all the devices from the network and then fires the event for updated list
        /// </summary>
        /// <param name="listUpdated"></param>
        public async void GetAllGigeDevicesInNetworkAsnyc(Action<List<CameraInformation>> listUpdated, string networkIP = "")
        {
            var list = await GetAllGigeDevicesInNetworkAsnyc().ConfigureAwait(false);
            listUpdated?.Invoke(list);
        }

        /// <summary>
        /// It will get all the devices from the network and returns the Camera list
        /// </summary>
        public async Task<List<CameraInformation>> GetAllGigeDevicesInNetworkAsnyc(string networkIP = "")
        {
            cameraInfoList = new List<CameraInformation>();
            GvcpCommand discovery = new(GvcpCommandType.Discovery);
            var data = SendBroadCastPacket(discovery.CommandBytes, DiscoveryReception, networkIP);
            await data;
            return cameraInfoList;
        }

        public async Task<(IPValue pValue, IRegister register)> GetRegister(string name)
        {
            (IPValue pValue, IRegister register) tuple = new(null, null);
            var category = await xmlHelper.GetRegisterByName(name);
            if (category == null)
            {
                return tuple;
            }

            if (category.PValue is IPValue pValue)
            {
                tuple.pValue = pValue;
            }
            
            if (category.PValue is IRegister register)
            {
                tuple.register = register;
            }

            return tuple;
        }
        
        public async Task<ICategory> GetRegisterCategory(string name)
        {
            return await xmlHelper.GetRegisterByName(name);
        }

        /// <summary>
        /// Leaves to control if in control
        /// </summary>
        /// <returns>True if leaves the control</returns>
        public async Task<bool> LeaveControl()
        {
            IsKeepingAlive = false;
            while (isHeartBeatThreadRunning)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
            int retryCount = 3;
            GvcpReply reply;
            do
            {
                reply = await WriteRegisterAsync(GvcpRegister.GevCCP, 0).ConfigureAwait(false);
                if (--retryCount == 0)
                {
                    break;
                }
            }
            while (reply.Status != GvcpStatus.GEV_STATUS_SUCCESS);
            return true;
        }

        /// <summary>
        /// Reads all register of camera
        /// </summary>
        /// <param name="cameraIp">Camera IP</param>
        /// <returns>Register dictionary</returns>
        public async Task ReadAllRegisterAddressFromCameraAsync(string cameraIp)
        {
            try
            {
                IsLoadingXml = true;

                if (!ValidateIp(cameraIp)) throw new InvalidIpException();

                //loading the XML file
                XmlDocument xml = new XmlDocument();
                xml.Load(await GetXmlFileFromCamera(cameraIp).ConfigureAwait(false));
                var xmlHelper = new XmlHelper(xml, new GenPort(this));
                await xmlHelper.LoadUp();
                CategoryDictionary = xmlHelper.CategoryDictionary;

                if (xmlHelper.CategoryDictionary != null)
                {
                    if (xmlHelper.CategoryDictionary.Count > 0)
                    {
                        RegistersDictionary = new Dictionary<string, (IPValue, IRegister)>();
                        await ReadAllRegisters(CategoryDictionary);
                    }
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                IsLoadingXml = false;
            }
        }

        /// <summary>
        /// Reads all register of camera
        /// </summary>
        /// <returns>Register dictionary</returns>
        public async Task ReadAllRegisterAddressFromCameraAsync()
        {
            await ReadAllRegisterAddressFromCameraAsync(CameraIp);
        }

        public async Task ReadAllRegisterAddressFromCameraAsync(IGvcp gvcp)
        {
            await ReadAllRegisterAddressFromCameraAsync(gvcp.CameraIp);
        }

        public async Task<GvcpReply> ReadMemoryAsync(string ip, byte[] memoryAddress, ushort count)
        {
            if (ValidateIp(ip))
            {
                GvcpCommand command = new(memoryAddress, GvcpCommandType.ReadMem, requestID: gvcpRequestID++, count: count);
                using UdpClient socket = new();
                socket.Client.ReceiveTimeout = ReceiveTimeoutInMilliseconds;
                socket.Connect(ip, 3956);
                return await SendGvcpCommand(socket, command).ConfigureAwait(false);
            }
            else
            {
                return new GvcpReply() { Error = "IP is not valid" };
            }
        }

        public async Task<GvcpReply> ReadMemoryAsync(string memoryAddressOrKey, ushort count)
        {
            return await ReadMemoryAsync(CameraIp, Converter.RegisterStringToByteArray(memoryAddressOrKey), count).ConfigureAwait(false);
        }

        /// <summary>
        /// Read Register
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="registerAddress"></param>
        /// <returns>Command Reply</returns>
        public async Task<GvcpReply> ReadRegisterAsync(string ip, byte[] registerAddress)
        {
            if (ValidateIp(ip))
            {
                GvcpCommand command = new(registerAddress, GvcpCommandType.ReadReg, requestID: gvcpRequestID++);
                using UdpClient socket = new();
                socket.Client.ReceiveTimeout = 1000;
                socket.Connect(ip, 3956);
                return await SendGvcpCommand(socket, command).ConfigureAwait(false);
            }
            else
            {
                return new GvcpReply() { Error = "IP is not valid" };
            }
        }

        /// <summary>
        /// Read Register
        /// </summary>
        /// <returns>Command Reply</returns>
        public async Task<GvcpReply> ReadRegisterAsync(GvcpRegister register)
        {
            return await ReadRegisterAsync(Converter.RegisterStringToByteArray(register.ToString("X"))).ConfigureAwait(false);
        }

        /// <summary>
        /// Read Register
        /// </summary>
        /// <returns>Command Reply</returns>
        public async Task<GvcpReply> ReadRegisterAsync(string Ip, string registerAddress)
        {
            return await ReadRegisterAsync(Ip, Converter.RegisterStringToByteArray(registerAddress)).ConfigureAwait(false);
        }

        /// <summary>
        /// Read Register
        /// </summary>
        /// <returns>Command Reply</returns>
        public async Task<GvcpReply> ReadRegisterAsync(byte[] registerAddress)
        {
            return await ReadRegisterAsync(CameraIp, registerAddress).ConfigureAwait(false);
        }

        /// <summary>
        /// Read Register
        /// </summary>
        /// <returns>Command Reply</returns>
        public async Task<GvcpReply> ReadRegisterAsync(string registerAddressOrKey)
        {
            return await ReadRegisterAsync(CameraIp, Converter.RegisterStringToByteArray(registerAddressOrKey)).ConfigureAwait(false);
        }

        /// <summary>
        /// Read Register
        /// </summary>
        /// <returns>Command Reply</returns>
        public async Task<GvcpReply> ReadRegisterAsync(string Ip, string[] registerAddresses)
        {
            return await ReadRegisterAsync(Ip, Converter.RegisterStringsToByteArray(registerAddresses)).ConfigureAwait(false);
        }

        /// <summary>
        /// Read Register
        /// </summary>
        /// <returns>Command Reply</returns>
        public async Task<GvcpReply> ReadRegisterAsync(string[] registerAddresses)
        {
            return await ReadRegisterAsync(CameraIp, Converter.RegisterStringsToByteArray(registerAddresses)).ConfigureAwait(false);
        }

        /// <summary>
        /// Read Register
        /// </summary>
        /// <returns>Command Reply</returns>
        public async Task<GvcpReply> ReadRegisterAsync(string Ip, GvcpRegister register)
        {
            return await ReadRegisterAsync(Ip, register.ToString("X")).ConfigureAwait(false);
        }

        public async Task<bool> ReadXmlFileAsync(string ip = null)
        {
            if (string.IsNullOrEmpty(ip))
            {
                ip = CameraIp;
            }
            else
            {
                if (ValidateIp(ip) is false)
                {
                    throw new InvalidIpException();
                }
            }
            XmlDocument xml = new XmlDocument();
            xml.Load(await GetXmlFileFromCamera(ip).ConfigureAwait(false));
            xmlHelper = new XmlHelper(xml, new GenPort(this));
            try
            {
                await xmlHelper.LoadUp();
                IsXmlFileLoaded = true;
            }
            catch (Exception)
            {
                IsXmlFileLoaded = false;
            }

            return IsXmlFileLoaded;
        }

        /// <summary>
        /// Takes control of the devices
        /// </summary>
        /// <param name="KeepAlive">
        /// If true thread will continuously send heartbeat command to keep the devices in control
        /// </param>
        /// <returns>Control Status</returns>
        public async Task<bool> TakeControl(bool KeepAlive = true)
        {
            bool controlStatus = false;
            Reconnect();
            if (await GetControlAsync(ControlSocket).ConfigureAwait(false))
            {
                controlStatus = true;
            }
            else
            {
                return controlStatus;
            }
            if (KeepAlive)
            {
                GvcpReply reply;
                int retryCount = 5;
                do
                {
                    reply = await WriteRegisterAsync(GvcpRegister.HeartbeatTimeout, 10000).ConfigureAwait(false);
                    if (--retryCount == 0)
                    {
                        break;
                    }
                }
                while (reply.Status != GvcpStatus.GEV_STATUS_SUCCESS);
                IsKeepingAlive = true;
                RunHeartbeatThread();
            }
            return controlStatus;
        }

        /// <summary>
        /// Write Memory
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<GvcpReply> WriteMemoryAsync(string memoryAddress, uint valueToWrite)
        {
            GvcpCommand gvcpCommand = new GvcpCommand(Converter.RegisterStringToByteArray(memoryAddress), GvcpCommandType.WriteMem, valueToWrite, gvcpRequestID++);
            return await WriteMemory(ControlSocket, gvcpCommand).ConfigureAwait(false);
        }

        /// <summary>
        /// Write Register
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<GvcpReply> WriteRegisterAsync(UdpClient socket, byte[] registerAddress, uint valueToWrite)
        {
            GvcpCommand gvcpCommand = new GvcpCommand(registerAddress, GvcpCommandType.WriteReg, valueToWrite, gvcpRequestID++);
            return await WriteRegister(socket, gvcpCommand).ConfigureAwait(false);
        }

        /// <summary>
        /// Write Register
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<GvcpReply> WriteRegisterAsync(UdpClient socket, string registerAddress, uint valueToWrite)
        {
            GvcpCommand gvcpCommand = new GvcpCommand(Converter.RegisterStringToByteArray(registerAddress), GvcpCommandType.WriteReg, valueToWrite, gvcpRequestID++);
            return await WriteRegister(socket, gvcpCommand).ConfigureAwait(false);
        }

        /// <summary>
        /// Write Register
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<GvcpReply> WriteRegisterAsync(UdpClient socket, string[] registerAddress, uint[] valuesToWrite)
        {
            GvcpCommand gvcpCommand = new GvcpCommand(registerAddress, valuesToWrite, gvcpRequestID++);
            return await WriteRegister(socket, gvcpCommand).ConfigureAwait(false);
        }

        /// <summary>
        /// Write Register
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<GvcpReply> WriteRegisterAsync(UdpClient socket, GvcpRegister register, uint valueToWrite)
        {
            GvcpCommand gvcpCommand = new GvcpCommand(Converter.RegisterStringToByteArray(register.ToString("X")), GvcpCommandType.WriteReg, valueToWrite, gvcpRequestID++);
            return await WriteRegister(socket, gvcpCommand).ConfigureAwait(false);
        }

        /// <summary>
        /// Write Register
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<GvcpReply> WriteRegisterAsync(string Ip, byte[] registerAddress, uint valueToWrite)
        {
            UdpClient socket = new UdpClient(Ip, PortGvcp);
            socket.Client.ReceiveTimeout = 1000;
            if (await GetControlAsync(socket).ConfigureAwait(false))
            {
                GvcpCommand gvcpCommand = new GvcpCommand(registerAddress, GvcpCommandType.WriteReg, valueToWrite, gvcpRequestID++);
                return await WriteRegister(socket, gvcpCommand).ConfigureAwait(false);
            }
            else
            {
                return new GvcpReply() { Status = GvcpStatus.GEV_STATUS_ACCESS_DENIED };
            }
        }

        /// <summary>
        /// Write Register
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<GvcpReply> WriteRegisterAsync(string Ip, string registerAddress, uint valueToWrite)
        {
            UdpClient socket = new UdpClient(Ip, PortGvcp);
            socket.Client.ReceiveTimeout = 1000;
            if (await GetControlAsync(socket).ConfigureAwait(false))
            {
                GvcpCommand gvcpCommand = new GvcpCommand(Converter.RegisterStringToByteArray(registerAddress), GvcpCommandType.WriteReg, valueToWrite, gvcpRequestID++);
                return await WriteRegister(socket, gvcpCommand).ConfigureAwait(false);
            }
            else
            {
                return new GvcpReply() { Status = GvcpStatus.GEV_STATUS_ACCESS_DENIED };
            }
        }

        /// <summary>
        /// Write Register
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<GvcpReply> WriteRegisterAsync(string Ip, string[] registerAddress, uint[] valuesToWrite)
        {
            // await Task.Delay(100).ConfigureAwait(false);
            UdpClient socket = new UdpClient(Ip, PortGvcp);
            socket.Client.ReceiveTimeout = 1000;
            if (await GetControlAsync(socket).ConfigureAwait(false))
            {
                GvcpCommand gvcpCommand = new GvcpCommand(registerAddress, valuesToWrite, gvcpRequestID++);
                return await WriteRegister(socket, gvcpCommand).ConfigureAwait(false);
            }
            else
            {
                return new GvcpReply() { Status = GvcpStatus.GEV_STATUS_ACCESS_DENIED };
            }
        }

        /// <summary>
        /// Write Register
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<GvcpReply> WriteRegisterAsync(string Ip, GvcpRegister register, uint valueToWrite)
        {
            UdpClient socket = new UdpClient(Ip, PortGvcp);
            socket.Client.ReceiveTimeout = 1000;
            if (await GetControlAsync(socket).ConfigureAwait(false))
            {
                GvcpCommand gvcpCommand = new GvcpCommand(Converter.RegisterStringToByteArray(register.ToString("X")), GvcpCommandType.WriteReg, valueToWrite, gvcpRequestID++);
                return await WriteRegister(socket, gvcpCommand).ConfigureAwait(false);
            }
            else
            {
                return new GvcpReply() { Status = GvcpStatus.GEV_STATUS_ACCESS_DENIED };
            }
        }

        /// <summary>
        /// Write Register
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<GvcpReply> WriteRegisterAsync(byte[] registerAddressOrKey, uint valueToWrite)
        {
            GvcpCommand gvcpCommand = new GvcpCommand(registerAddressOrKey, GvcpCommandType.WriteReg, valueToWrite, gvcpRequestID++);
            return await WriteRegister(ControlSocket, gvcpCommand).ConfigureAwait(false);
        }

        /// <summary>
        /// Write Register
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<GvcpReply> WriteRegisterAsync(string registerAddress, uint valueToWrite)
        {
            GvcpCommand gvcpCommand = new GvcpCommand(Converter.RegisterStringToByteArray(registerAddress), GvcpCommandType.WriteReg, valueToWrite, gvcpRequestID++);
            return await WriteRegister(ControlSocket, gvcpCommand).ConfigureAwait(false);
        }

        /// <summary>
        /// Write Register
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<GvcpReply> WriteRegisterAsync(string[] registerAddress, uint[] valueToWrite)
        {
            GvcpCommand gvcpCommand = new GvcpCommand(registerAddress, valueToWrite, gvcpRequestID++);
            return await WriteRegister(ControlSocket, gvcpCommand).ConfigureAwait(false);
        }

        /// <summary>
        /// Write Register
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<GvcpReply> WriteRegisterAsync(GvcpRegister register, uint valueToWrite)
        {
            GvcpCommand gvcpCommand = new GvcpCommand(Converter.RegisterStringToByteArray(register.ToString("X")), GvcpCommandType.WriteReg, valueToWrite, gvcpRequestID++);
            return await WriteRegister(ControlSocket, gvcpCommand).ConfigureAwait(false);
        }

        private static async Task<GvcpReply> SendGvcpCommand(UdpClient socketTx, GvcpCommand command)
        {
            GvcpReply gvcpReply = new();
            await socketTx.SendAsync(command.CommandBytes, command.Length).ConfigureAwait(false);
            gvcpReply.IsSent = true;
            Task<UdpReceiveResult> reply = socketTx.ReceiveAsync();
            if (await Task.WhenAny(reply, Task.Delay(socketTx.Client.ReceiveTimeout)).ConfigureAwait(false) == reply)
            {
                gvcpReply.DetectCommand(reply.Result.Buffer);
                gvcpReply.IPSender = reply.Result.RemoteEndPoint.Address.ToString();
                gvcpReply.PortSender = reply.Result.RemoteEndPoint.Port;
            }
            return gvcpReply;
        }

        /// <summary>
        /// This function will send UDP packet to the socket (IP/port)
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="inputCommand"></param>
        /// <param name="replySize"></param>
        /// <returns></returns>
        private static async Task<byte[]> SendUdp(UdpClient socket, byte[] inputCommand, bool replyRequired = false, string optionalCommandNameForInformation = "UDP")
        {
            var reply = Array.Empty<byte>();
            var task = new Task(
                delegate
                {
                    try
                    {
                        socket.Send(inputCommand, inputCommand.Length);
                        if (replyRequired)
                        {
                            var endPointMCU = new IPEndPoint(IPAddress.Any, ((IPEndPoint)socket.Client.RemoteEndPoint).Port);
                            reply = socket.Receive(ref endPointMCU);
                        }
                    }
                    catch (Exception)
                    {
                        reply = null;
                    }
                });
            task.Start();
            await task.ConfigureAwait(false);
            return reply;
        }

        private static Stream UnZipEncodedZipFile(byte[] encodedZipFile)
        {
            var xmlZipStream = new MemoryStream(encodedZipFile);
            try
            {
                ZipArchive zipFile = new ZipArchive(xmlZipStream, ZipArchiveMode.Read, false);
                Stream unZipFile = new MemoryStream();

                foreach (var entry in zipFile.Entries)
                {
                    if (entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    {
                        unZipFile = entry.Open();
                    }
                }
                return unZipFile;
            }
            catch
            {
                throw;
            }
        }

        private CameraInformation DecodeDiscoveryPacket(byte[] discoveryPacket)
        {
            var cameraInfo = new CameraInformation()
            {
                IP = discoveryPacket[44].ToString() + "." + discoveryPacket[45].ToString() + "." + discoveryPacket[46].ToString() + "." + discoveryPacket[47].ToString()
            };

            StringBuilder MAC = new StringBuilder(18);
            for (int index = 0; index < 6; index++)
            {
                MAC.AppendFormat("{0:x2}", discoveryPacket[18 + index]);
                MAC.Append(":");
            }
            MAC.Remove(17, 1);//Removing the last colon

            Array.Copy(discoveryPacket, 18, cameraInfo.MacAddress, 0, 6);
            cameraInfo.MAC = MAC.ToString();

            var manufacturer = new byte[32];
            Array.Copy(discoveryPacket, 80, manufacturer, 0, 32);
            cameraInfo.ManufacturerName = Encoding.ASCII.GetString(manufacturer);
            cameraInfo.ManufacturerName = cameraInfo.ManufacturerName.Replace("\0", "");

            var model = new byte[32];
            Array.Copy(discoveryPacket, 80 + 32, model, 0, 32);
            cameraInfo.Model = Encoding.ASCII.GetString(model);
            cameraInfo.Model = cameraInfo.Model.Replace("\0", "");

            var version = new byte[32];
            Array.Copy(discoveryPacket, 80 + 32 + 32, version, 0, 32);
            cameraInfo.Version = Encoding.ASCII.GetString(version);
            cameraInfo.Version = cameraInfo.Version.Replace("\0", "");

            var manufacturerSpecificInfo = new byte[48];
            Array.Copy(discoveryPacket, 80 + 32 + 32 + 32, manufacturerSpecificInfo, 0, 48);
            cameraInfo.ManufacturerSpecificInformation = Encoding.ASCII.GetString(manufacturerSpecificInfo);
            cameraInfo.ManufacturerSpecificInformation = cameraInfo.ManufacturerSpecificInformation.Replace("\0", "");

            var serialNumber = new byte[16];
            Array.Copy(discoveryPacket, 80 + 32 + 32 + 32 + 48, serialNumber, 0, 16);
            cameraInfo.SerialNumber = Encoding.ASCII.GetString(serialNumber);
            cameraInfo.SerialNumber = cameraInfo.SerialNumber.Replace("\0", "");

            var userDefinedName = new byte[16];
            Array.Copy(discoveryPacket, 80 + 32 + 32 + 32 + 48 + 16, userDefinedName, 0, 16);
            cameraInfo.UserDefinedName = Encoding.ASCII.GetString(userDefinedName);
            cameraInfo.UserDefinedName = cameraInfo.UserDefinedName.Replace("\0", "");
            return cameraInfo;
        }

        /// <summary>
        /// Deconds and Add camera in camera list
        /// </summary>
        /// <param name="localEndpoint">Sender End point</param>
        /// <param name="data">Data</param>
        private void DiscoveryReception(IPEndPoint localEndpoint, byte[] data)
        {
            var camera = DecodeDiscoveryPacket(data);
            camera.NetworkIP = localEndpoint.Address.ToString();
            cameraInfoList.Add(camera);
        }

        #region Read All Registers Address XML

        private async Task<bool> GetControlAsync(UdpClient socket)
        {
            var currentStatus = await ReadRegisterAsync(Converter.RegisterStringToByteArray(GvcpRegister.GevCCP.ToString("X"))).ConfigureAwait(false);
            if (currentStatus.IsValid)
            {
                if (currentStatus.RegisterValue == 0)//Its free and can be controlled
                {
                    GvcpCommand controlCommand = new(Converter.RegisterStringToByteArray(GvcpRegister.GevCCP.ToString("X")),
                        GvcpCommandType.WriteReg, 2, gvcpRequestID++);
                    var reply = await SendGvcpCommand(socket, controlCommand).ConfigureAwait(false);
                    return reply.Status == GvcpStatus.GEV_STATUS_SUCCESS;
                }
            }
            return false;
        }

        private (string, int, int) GetFileDetails(string Message)
        {
            string[] words = Message.Split(';');
            string fileName = words[0].Remove(0, 6);
            int fileAddress = Int32.Parse(words[1], System.Globalization.NumberStyles.HexNumber);
            int fileLength = Int32.Parse(words[2], System.Globalization.NumberStyles.HexNumber);

            return (fileName, fileAddress, fileLength);
        }

        private async Task<(byte[] data, string fileName)> GetRawXmlFileFromCamera(string IP)
        {
            return await Task.Run(() =>
            {
                UdpClient client = new UdpClient();
                client.Client.ReceiveTimeout = 1000;
                //connecting to the server
                client.Connect(IP, PortGvcp);

                byte[] commandCCP = new byte[] { 0x42, 0x00, 0x00, 0x82, 0x00, 0x08, 0x10, 0x01, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x00, 0x00, 0x02 };

                //sending the packet
                //  client.Send(commandCCP, commandCCP.Length);
                Task.Delay(100);
                //preparing the header for sending

                byte[] gvcpHeader = GetReadMessageHeader(0x0200); //GevFirstURL = 0x0200

                //sending the packet
                client.Send(gvcpHeader, gvcpHeader.Length);
                gvcpRequestID++;

                int packetSize = 512 + 24; //512 for the original payload size and 24  for  the header
                byte[] count = { 0x02, 0x18 }; //Number of bytes to read from device memory it must be multiple of 4 bytes

                IPEndPoint server = new IPEndPoint(IPAddress.Parse(IP), PortGvcp);
                byte[] recivedData = client.Receive(ref server);
                if (recivedData.Length < 12) throw new Exception("Access denied");
                string localFile = Encoding.ASCII.GetString(recivedData, 12, recivedData.Length - 12);

                var (fileName, fileAddress, fileLength) = GetFileDetails(localFile);

                //finding the last packet length
                var lastPacket = (fileLength % packetSize);

                if (lastPacket % 4 != 0)
                {
                    fileLength -= lastPacket;
                    double tempLastPcaket = ((double)lastPacket / 4);
                    lastPacket = (int)(Math.Ceiling(tempLastPcaket) * 4);
                    fileLength += lastPacket;
                }
                byte[] encodedZipFile = new byte[fileLength];

                if (recivedData[0] == (byte)0x00)
                {
                    for (int i = 0; i < fileLength; i += packetSize)
                    {
                        count = i == (fileLength - lastPacket) ? BitConverter.GetBytes(lastPacket) : BitConverter.GetBytes(packetSize);

                        byte[] requestID = BitConverter.GetBytes(gvcpRequestID);
                        byte[] tempFileAddress = BitConverter.GetBytes(fileAddress + i);

                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(tempFileAddress);
                            Array.Reverse(requestID);
                            Array.Reverse(count);
                        }

                        byte[] readFileHeader = { 0x42, 0x01, 0x00, 0x84, 0x00, 0x08,
                                requestID[0], requestID[1],
                                tempFileAddress[0], tempFileAddress[1], tempFileAddress[2], tempFileAddress[3],
                                count[0], count[1], count[2], count[3] };
                        client.Send(readFileHeader, readFileHeader.Length);

                        recivedData = client.Receive(ref server);

                        if (recivedData[0] == (byte)0x00)
                        {
                            Array.Copy(recivedData, 12, encodedZipFile, i, recivedData.Length - 12);
                            gvcpRequestID++;
                        }
                        else
                            break;
                    }
                }
                return (encodedZipFile, fileName);
            }).ConfigureAwait(false);
        }

        private byte[] GetReadMessageHeader(int register)
        {
            //converting register and requestID into bytes
            byte[] tempRegister = BitConverter.GetBytes(register);
            byte[] requestID = BitConverter.GetBytes((short)gvcpRequestID);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(tempRegister);
                Array.Reverse(requestID);
            }

            //preparing the header for sending
            byte[] gvcpHeader = { 0x42, 0x01, 0x00, 0x84, 0x00, 0x08, requestID[0], requestID[1], 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x02, 0x00 };

            return gvcpHeader;
        }

        private async Task<Stream> GetXmlFileFromCamera(string ip)
        {
            // initializing the variables

            //loop to get the zip file data in bytes
            Stream xmlFile = new MemoryStream();
            (var fileData, var fileName) = await GetRawXmlFileFromCamera(ip).ConfigureAwait(false);
            fileData = TrimTrailingZeros(fileData);
            
            var fileNameParts = fileName.Split('.');
            if (fileData.Length != 0)
            {
                switch (fileNameParts[fileNameParts.Length - 1])
                {
                    case "xml":
                        xmlFile = new MemoryStream(fileData, 0, fileData.Length, writable: false, publiclyVisible: true);
                        break;

                    case "zip":
                        //converting the zip file from bytes to stream
                        xmlFile = UnZipEncodedZipFile(fileData);
                        break;

                    default:
                        break;
                }
            }

            gvcpRequestID++;
            return xmlFile;
        }

        static byte[] TrimTrailingZeros(byte[] buf)
        {
            int i = buf.Length - 1;           // start at the last byte
            while (i >= 0 && buf[i] == 0)     // walk backwards while bytes are 0x00
                i--;

            // If nothing was trimmed, return the original array to avoid allocating.
            if (i == buf.Length - 1) return buf;

            // Otherwise, return a new array containing bytes [0..i]
            return buf.AsSpan(0, i + 1).ToArray();
        }


        public async Task SaveXmlFileFromCamera(string path, string ip = null)
        {
            if (string.IsNullOrEmpty(ip))
            {
                ip = CameraIp;
            }
            else
            {
                if (ValidateIp(ip) is false)
                {
                    throw new InvalidIpException();
                }
            }

            (var fileData, var fileName) = await GetRawXmlFileFromCamera(ip).ConfigureAwait(false);
            var filePath = Path.Combine(path, fileName);
            try
            {
                File.WriteAllBytes(filePath, fileData);
            }
            catch (Exception ex)
            {

            }
        }

        private async Task ReadAllRegisters(List<ICategory> categories)
        {
            if (categories == null)
                return;
            foreach (var category in categories)
            {
                if (category == null)
                    continue;

                if (category.PFeatures != null)
                    await ReadAllRegisters(category.PFeatures);
                if (!RegistersDictionary.ContainsKey(category.CategoryProperties.Name))
                {
                    (IPValue, IRegister) tuple = new(null, null);
                    if (category.PValue is IPValue pValue)
                    {
                        tuple.Item1 = pValue;
                    }
                    if (category.PValue is IRegister register)
                    {
                        tuple.Item2 = register;
                    }
                    RegistersDictionary.Add(category.CategoryProperties.Name, tuple);
                }
            }
        }

        #endregion Read All Registers Address XML

        #endregion Status Commands

        #region Write Register

        private void Reconnect()
        {
            try
            {
                try
                {
                    ControlSocket?.Client.Close();
                    ControlSocket?.Close();
                }
                catch (Exception)
                {
                }
                try
                {
                    ControlSocket = new UdpClient(cameraIP, PortGvcp);
                    ControlSocket.Client.ReceiveTimeout = 1000;
                    ControlSocket.Client.SendTimeout = 500;
                }
                catch (Exception)
                {
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void RunHeartbeatThread()
        {
            Task.Run(async () =>
            {
                isHeartBeatThreadRunning = true;
                GvcpCommand command = new(Converter.RegisterStringToByteArray(GvcpRegister.GevCCP.ToString("X")), GvcpCommandType.ReadReg);
                while (IsKeepingAlive)
                {
                    try
                    {
                        _ = SendGvcpCommand(ControlSocket, command);
                        if (!IsKeepingAlive) break;
                        await Task.Delay(500).ConfigureAwait(false);
                        if (!IsKeepingAlive) break;
                        await Task.Delay(500).ConfigureAwait(false);
                        ElapsedOneSecond?.Invoke(null, null);
                        if (!IsKeepingAlive) break;
                        await Task.Delay(500).ConfigureAwait(false);
                        if (!IsKeepingAlive) break;
                        await Task.Delay(500).ConfigureAwait(false);
                        ElapsedOneSecond?.Invoke(null, null);
                    }
                    catch (Exception ex)
                    {
                        isHeartBeatThreadRunning = false;
                    }
                }
                isHeartBeatThreadRunning = false;
            });
        }

        /// <summary>
        /// This function will send Broadcast packet to the socket (IP/port)
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="receptionEvent"></param>
        /// <param name="ipNetwork"></param>
        private async Task SendBroadCastPacket(byte[] packet, Action<IPEndPoint, byte[]> receptionEvent, string ipNetworkFixed = "", string allowedNetMask = "*")
        {
            var reply = Array.Empty<byte>();
            List<IPAddress> ips = new();
            if (IPAddress.TryParse(ipNetworkFixed, out IPAddress ip))
            {
                ips.Add(ip);
            }
            else
            {
                var ipsString = NetworkService.GetAllInterfaces(allowedNetMask);
                foreach (var ipString in ipsString)
                {
                    if (IPAddress.TryParse(ipString, out IPAddress ipToAdd))
                    {
                        ips.Add(ipToAdd);
                    }
                }
            }
            foreach (var ipNetwork in ips)
            {
                var broadcastClientLocal = new UdpClient()
                {
                    EnableBroadcast = true,
                };
                broadcastClientLocal.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                var endPoint2 = new IPEndPoint(ipNetwork, 0);
                broadcastClientLocal.Client.Bind(endPoint2);
                var endPoint = new IPEndPoint(IPAddress.Broadcast, PortGvcp);
                broadcastClientLocal.Send(packet, packet.Length, endPoint);
                broadcastClientLocal.Client.ReceiveTimeout = 500;
                await Task.Run(() =>
                {
                    while (true)
                    {
                        try
                        {
                            IPEndPoint endPointRx = null;
                            var data = broadcastClientLocal.Receive(ref endPointRx);
                            receptionEvent?.Invoke((IPEndPoint)(broadcastClientLocal.Client.LocalEndPoint), data);
                        }
                        catch (Exception)
                        {
                            break;
                        }
                    }
                });
            }
        }

        private bool ValidateIp(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }
            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }
            return splitValues.All(r => byte.TryParse(r, out byte tempForParsing));
        }

        /// <summary>
        /// Write Memory
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="gvcpCommand"></param>
        /// <returns></returns>
        private async Task<GvcpReply> WriteMemory(UdpClient socket, GvcpCommand gvcpCommand)
        {
            await socket.SendAsync(gvcpCommand.CommandBytes, gvcpCommand.Length).ConfigureAwait(false);
            var reply = await socket.ReceiveAsync().ConfigureAwait(false);
            if (reply.Buffer?.Length > 0)
            {
                return new GvcpReply(reply.Buffer);
            }
            else
            {
                return new GvcpReply() { Error = "Couldn't Get Reply" };
            }
        }

        /// <summary>
        /// Write Register
        /// </summary>
        /// <returns>Command Status</returns>
        private async Task<GvcpReply> WriteRegister(UdpClient socket, GvcpCommand gvcpCommand)
        {
            return await SendGvcpCommand(socket, gvcpCommand).ConfigureAwait(false);
        }

        #endregion Write Register
    }
}