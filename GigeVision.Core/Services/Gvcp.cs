using GenICam;
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

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Max GVCP payload = 540 bytes and must be multiple of 32
    /// </summary>
    public class Gvcp : IGvcp
    {
        private string cameraIP = "";

        private ushort gvcpRequestID = 1;

        /// <summary>
        /// GVCP Port
        /// </summary>
        public int PortGvcp { get => 3956; }

        #region Constructor

        /// <summary>
        /// Gvcp constructor, initializes camera IP, and try to get register values
        /// </summary>
        /// <param name="ip"></param>
        public Gvcp(string ip)
        {
            CameraIp = ip;
        }

        /// <summary>
        /// Default GVCP constructor
        /// </summary>
        public Gvcp()
        {
        }

        #endregion Constructor

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
        /// Controlling port of GVCP
        /// </summary>
        public int PortControl { get; set; }

        /// <summary>
        /// Control socket
        /// </summary>
        public UdpClient ControlSocket { get; private set; }

        /// <summary>
        /// If true, heartbeat command will be sent to the devices after regular interval
        /// </summary>

        public bool IsKeepingAlive { get; set; }

        /// <summary>
        /// It can be for any thing, to update fps to check devices
        /// </summary>
        public EventHandler ElapsedOneSecond { get; set; }

        /// <summary>
        /// Event fired whenever camera IP changed: used to get registers
        /// </summary>
        public EventHandler CameraIpChanged { get; set; }

        public List<ICategory> CategoryDictionary { get; private set; }

        #region Status Commands

        public Dictionary<string, string> RegistersDictionary { get; set; }

        public Dictionary<string, IPValue> RegistersDictionaryValues { get; set; }

        /// <summary>
        /// Check camera status
        /// </summary>
        /// <param name="ip">IP Camera</param>
        /// <returns>Camera Status: Available/InControl or Unavailable</returns>
        public async Task<CameraStatus> CheckCameraStatusAsync(string ip)
        {
            if (ValidateIp(ip))
            {
                var cameraStatusPacket = await ReadRegisterAsync(ip, GvcpRegister.CCP).ConfigureAwait(false);
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
        public async Task<bool> ForceIPAsync(byte[] macAddress, string iPToSet)
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

            var maskBytes = BitConverter.GetBytes(Converter.ConvertIpToNumber("255.255.255.0"));
            Array.Reverse(maskBytes, 0, 4);
            Array.Copy(maskBytes, 0, forceIpCommand, 44, 4);//4bytes, TotalLength= 48 + (12 reserved bytes) = 60

            var gateWayBytes = new byte[4];
            Array.Copy(ipBytes, 0, gateWayBytes, 0, 4);
            gateWayBytes[3] = 0x01;
            Array.Copy(gateWayBytes, 0, forceIpCommand, 60, 4);//4bytes, TotalLength= 64

            using var client = new UdpClient();
            client.Connect(IPAddress.Broadcast, 3956);
            client.Client.SendTimeout = 100;
            client.Client.ReceiveTimeout = 500;
            var reply = await SendUdp(client, forceIpCommand, true).ConfigureAwait(false);
            if (reply?.Length > 5)
            {
                if (reply[3] == 0x05 && reply[0] == 0 && reply[1] == 0) //ForceIp acknowledgment
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Forces the IP to the camera
        /// </summary>
        /// <param name="macAddress">Mac Address of Camera</param>
        /// <param name="iPToSet">IP to set</param>
        /// <returns></returns>
        public async Task<bool> ForceIPAsync(string macAddress, string iPToSet)
        {
            return await ForceIPAsync(Converter.HexStringToByteArray(macAddress), iPToSet).ConfigureAwait(false);
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
        /// It will get all the devices from the network and returns the list updated list
        /// </summary>
        public async Task<List<CameraInformation>> GetAllGigeDevicesInNetworkAsnyc(string networkIP = "")
        {
            var cameraInfoList = new List<CameraInformation>();
            try
            {
                using var socket = new UdpClient();
                if (string.IsNullOrEmpty(networkIP))
                {
                    using var socketIP = new UdpClient();
                    socketIP.Connect(IPAddress.Parse("8.8.8.8"), PortGvcp);
                    var ip = (socketIP.Client.LocalEndPoint as IPEndPoint)?.Address.GetAddressBytes();
                    ip[3] = 255;
                    socketIP.Close();
                    socketIP.Dispose();
                    socket.Connect(new IPAddress(ip), PortGvcp);
                }
                else
                {
                    var ip = IPAddress.Parse(networkIP).GetAddressBytes();
                    ip[3] = 255;
                    socket.Connect(new IPAddress(ip), PortGvcp);
                }
                socket.Client.SendTimeout = 100;
                GvcpCommand discovery = new(GvcpCommandType.Discovery);
                socket.Send(discovery.CommandBytes, discovery.Length);
                int port = ((IPEndPoint)socket.Client.LocalEndPoint).Port;
                using UdpClient udpClient = new();
                socket.Close();
                socket.Dispose();
                var endPoint = new IPEndPoint(IPAddress.Any, port);
                udpClient.Client.Bind(endPoint);
                while (true)//listen for devices
                {
                    Task<UdpReceiveResult> taskRecievePacket = udpClient.ReceiveAsync();
                    if (await Task.WhenAny(taskRecievePacket, Task.Delay(500)).ConfigureAwait(false) == taskRecievePacket)
                    {
                        if (taskRecievePacket.Result.Buffer.Length > 255)
                            cameraInfoList.Add(DecodeDiscoveryPacket(taskRecievePacket.Result.Buffer));
                    }
                    else
                    {
                        udpClient.Close();
                        udpClient.Dispose();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return cameraInfoList;
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

        #region Read All Registers Address XML

        /// <summary>
        /// Reads all register of camera
        /// </summary>
        /// <param name="cameraIp">Camera IP</param>
        /// <returns>Register dictionary</returns>
        public async Task<Dictionary<string, string>> ReadAllRegisterAddressFromCameraAsync(string cameraIp)
        {

            if (!ValidateIp(CameraIp)) throw new InvalidIpException();

            //loading the XML file
            XmlDocument xml = new XmlDocument();
            xml.Load(await GetXmlFileFromCamera(cameraIp).ConfigureAwait(false));

            var xmlHelper = new XmlHelper("Category", xml, new GenPort(this));
            CategoryDictionary = xmlHelper.CategoryDictionary;


            if (xmlHelper.CategoryDictionary != null)
            {
                if (xmlHelper.CategoryDictionary.Count > 0)
                {
                    RegistersDictionary = new Dictionary<string, string>();
                    RegistersDictionaryValues = new Dictionary<string, IPValue>();
                    RegistersDictionary.Add("XmlVersion", xmlHelper.Xmlns.InnerText);
                    ReadAllRegisters(CategoryDictionary);
                }
            }

            return RegistersDictionary;
        }

        /// <summary>
        /// Reads all register of camera
        /// </summary>
        /// <returns>Register dictionary</returns>
        public async Task<Dictionary<string, string>> ReadAllRegisterAddressFromCameraAsync()
        {
            return await ReadAllRegisterAddressFromCameraAsync(CameraIp).ConfigureAwait(false);
        }

        public async Task<Dictionary<string, string>> ReadAllRegisterAddressFromCameraAsync(IGvcp gvcp)
        {

            if (!ValidateIp(gvcp.CameraIp)) throw new InvalidIpException();

            //loading the XML file
            XmlDocument xml = new XmlDocument();
            xml.Load(await GetXmlFileFromCamera(gvcp.CameraIp).ConfigureAwait(false));

            //handling the name-space of the XML file to cover all the cases
            var xmlHelper = new XmlHelper("Category", xml, new GenPort(this));
            CategoryDictionary = xmlHelper.CategoryDictionary;

            if (xmlHelper.CategoryDictionary != null)
            {
                if (xmlHelper.CategoryDictionary.Count > 0)
                {
                    RegistersDictionary = new Dictionary<string, string>();
                    RegistersDictionaryValues = new Dictionary<string, IPValue>();
                    RegistersDictionary.Add("XmlVersion", xmlHelper.Xmlns.InnerText);
                    ReadAllRegisters(CategoryDictionary);
                }
            }
            //finding the nodes and their values
            return RegistersDictionary;
        }

        private async void ReadAllRegisters(List<ICategory> categories)
        {
            if (categories == null)
                return;
            foreach (var category in categories)
            {
                if (category == null)
                    continue;

                if (category.PFeatures != null)
                    ReadAllRegisters(category.PFeatures);

                if (!RegistersDictionaryValues.ContainsKey(category.CategoryProperties.Name))
                    RegistersDictionaryValues.Add(category.CategoryProperties.Name, category.PValue);

                if (RegistersDictionary.ContainsKey(category.CategoryProperties.Name))
                    continue;
                else if (category is IGenRegister genRegister)
                {
                    if (RegistersDictionary.ContainsKey(category.CategoryProperties.Name))
                        continue;

                    RegistersDictionary.Add(category.CategoryProperties.Name, $"0x{ await genRegister.GetAddress().ConfigureAwait(false):X4}");
                }
                else if (category.PValue is IPValue pValue)
                {
                    if (pValue is IRegister register)
                    {
                        if (RegistersDictionary.ContainsKey(category.CategoryProperties.Name))
                            continue;

                        RegistersDictionary.Add(category.CategoryProperties.Name, $"0x{await register.GetAddress().ConfigureAwait(false):X4}");
                    }
                }
            }
        }

        private (string, int, int) GetFileDetails(string Message)
        {
            string[] words = Message.Split(';');
            string fileName = words[0].Remove(0, 6);
            int fileAddress = Int32.Parse(words[1], System.Globalization.NumberStyles.HexNumber);
            int fileLength = Int32.Parse(words[2], System.Globalization.NumberStyles.HexNumber);

            return (fileName, fileAddress, fileLength);
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

        private async Task<byte[]> GetRawXmlFileFromCamera(string IP)
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
                return encodedZipFile;
            }).ConfigureAwait(false);
        }

        private async Task<Stream> GetXmlFileFromCamera(string ip)
        {
            // initializing the variables
            Stream unZipFile = new MemoryStream();

            //loop to get the zip file data in bytes

            var encodedZipFile = await GetRawXmlFileFromCamera(ip).ConfigureAwait(false);
            //converting the zip file from bytes to stream
            if (encodedZipFile.Length != 0)
            {
                var xmlZipStream = new MemoryStream(encodedZipFile);
                try
                {
                    ZipArchive zipFile = new ZipArchive(xmlZipStream, ZipArchiveMode.Read, false);

                    foreach (var entry in zipFile.Entries)
                    {
                        if (entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        {
                            unZipFile = entry.Open();
                        }
                    }
                }
                catch
                {
                    throw;
                }
            }

            gvcpRequestID++;

            return unZipFile;
        }

        #endregion Read All Registers Address XML

        #endregion Status Commands

        #region ReadRegister

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

        #endregion ReadRegister

        #region Write Register

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

        /// <summary>
        /// Write Register
        /// </summary>
        /// <returns>Command Status</returns>
        private async Task<GvcpReply> WriteRegister(UdpClient socket, GvcpCommand gvcpCommand)
        {
            return await SendGvcpCommand(socket, gvcpCommand).ConfigureAwait(false);
        }

        private async Task<bool> GetControlAsync(UdpClient socket)
        {
            var currentStatus = await ReadRegisterAsync(Converter.RegisterStringToByteArray(GvcpRegister.CCP.ToString("X"))).ConfigureAwait(false);
            if (currentStatus.IsValid)
            {
                if (currentStatus.RegisterValue == 0)//Its free and can be controlled
                {
                    GvcpCommand controlCommand = new(Converter.RegisterStringToByteArray(GvcpRegister.CCP.ToString("X")),
                        GvcpCommandType.WriteReg, 2, gvcpRequestID++);
                    var reply = await SendGvcpCommand(socket, controlCommand).ConfigureAwait(false);
                    return reply.Status == GvcpStatus.GEV_STATUS_SUCCESS;
                }
            }
            return false;
        }

        #endregion Write Register

        #region ReadMemory

        public async Task<GvcpReply> ReadMemoryAsync(string ip, byte[] memoryAddress, ushort count)
        {
            if (ValidateIp(ip))
            {
                GvcpCommand command = new(memoryAddress, GvcpCommandType.ReadMem, requestID: gvcpRequestID++, count: count);
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

        public async Task<GvcpReply> ReadMemoryAsync(string memoryAddressOrKey, ushort count)
        {
            return await ReadMemoryAsync(CameraIp, Converter.RegisterStringToByteArray(memoryAddressOrKey), count).ConfigureAwait(false);
        }

        #endregion ReadMemory

        #region WriteMemory

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

        #endregion WriteMemory

        #region Common Methods

        private bool isHeartBeatThreadRunning;

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
                reply = await WriteRegisterAsync(GvcpRegister.CCP, 0).ConfigureAwait(false);
                if (--retryCount == 0)
                {
                    break;
                }
            }
            while (reply.Status != GvcpStatus.GEV_STATUS_SUCCESS);
            return true;
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
                GvcpCommand command = new(Converter.RegisterStringToByteArray(GvcpRegister.CCP.ToString("X")), GvcpCommandType.ReadReg);
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

        #endregion Common Methods
    }
}