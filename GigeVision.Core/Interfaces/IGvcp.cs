using GigeVision.Core.Enums;
using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GigeVision.Core.Interfaces
{
    public interface IGvcp
    {
        /// <summary>
        /// It can be for any thing, to update fps to check devices
        /// </summary>
        EventHandler ElapsedOneSecond { get; set; }

        EventHandler CameraIpChanged { get; set; }

        string CameraIp { get; set; }
        int PortControl { get; }
        bool IsKeepingAlive { get; }

        Dictionary<string, string> RegistersDictionary { get; set; }

        Task<GvcpReply> WriteRegisterAsync(UdpClient socket, byte[] registerAddress, uint valueToWrite);

        Task<GvcpReply> WriteRegisterAsync(UdpClient socket, string registerAddress, uint valueToWrite);

        Task<GvcpReply> WriteRegisterAsync(string Ip, byte[] registerAddress, uint valueToWrite);

        Task<GvcpReply> WriteRegisterAsync(string Ip, string registerAddress, uint valueToWrite);

        Task<GvcpReply> WriteRegisterAsync(byte[] registerAddressOrKey, uint valueToWrite);

        Task<GvcpReply> WriteRegisterAsync(string registerAddressOrKey, uint valueToWrite);

        Task<GvcpReply> WriteRegisterAsync(UdpClient socket, string[] registerAddress, uint[] valuesToWrite);

        Task<GvcpReply> WriteRegisterAsync(string Ip, string[] registerAddress, uint[] valuesToWrite);

        Task<GvcpReply> WriteRegisterAsync(string[] registerAddressOrKey, uint[] valueToWrite);

        Task<GvcpReply> WriteRegisterAsync(GvcpRegister register, uint valueToWrite);

        Task<GvcpReply> WriteRegisterAsync(string Ip, GvcpRegister register, uint valueToWrite);

        Task<GvcpReply> WriteRegisterAsync(UdpClient socket, GvcpRegister register, uint valueToWrite);

        Task<GvcpReply> ReadRegisterAsync(GvcpRegister register);

        Task<GvcpReply> ReadRegisterAsync(string Ip, GvcpRegister register);

        Task<GvcpReply> ReadRegisterAsync(string Ip, byte[] registerAddress);

        Task<GvcpReply> ReadRegisterAsync(string Ip, string registerAddress);

        Task<GvcpReply> ReadRegisterAsync(byte[] registerAddressOrKey);

        Task<GvcpReply> ReadRegisterAsync(string registerAddressOrKey);

        Task<GvcpReply> ReadRegisterAsync(string Ip, string[] registerAddresses);

        Task<GvcpReply> ReadRegisterAsync(string[] registerAddresses);

        Task<Dictionary<string, string>> ReadAllRegisterAddressFromCameraAsync(string cameraIp);

        Task<Dictionary<string, string>> ReadAllRegisterAddressFromCameraAsync();

        Task<bool> ForceIPAsync(byte[] macAddress, string iPToSet);

        Task<bool> ForceIPAsync(string macAddress, string iPToSet);

        Task<List<CameraInformation>> GetAllGigeDevicesInNetworkAsnyc();

        Task<CameraStatus> CheckCameraStatusAsync(string ip);

        Task<CameraStatus> CheckCameraStatusAsync();

        Task<bool> TakeControl(bool KeepAlive = true);

        Task<bool> LeaveControl();
    }
}