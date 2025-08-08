﻿using Crc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GigeVision.Core
{
    public class NetworkService
    {
        /// <summary>
        /// This method requires admin rights
        /// </summary>
        /// <returns>Status</returns>
        public static bool AllowAppThroughFirewall()
        {
            string appFullPath = Process.GetCurrentProcess().MainModule.FileName;
            string appName = Path.GetFileNameWithoutExtension(appFullPath);

            //Generating unique ID for this application
            Crc16Ccitt checksum = new();
            var crc = checksum.ComputeChecksum(appFullPath);
            string ruleName = $"Gvsp-{appName}{crc:X4}".Replace(" ", "");
            //Checking if we already have the access - only by checking the rule name is present or not
            var command = $"/C netsh advfirewall firewall show rule name = all | findstr  /r /s /i /m /c:\"\\<{ruleName}\\>\"";
            var reply = RunCommand(command);

            if (string.IsNullOrEmpty(reply))
            {
                command = $"/C netsh advfirewall firewall add rule name =\"{ruleName}\" dir=in action=allow program=\"{appFullPath}\" enable=yes";
                try
                {
                    RunCommandAdmin(command);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }

        /// Get all ethernet interfaces for the device
        /// </summary>
        /// <param name="ipVersion4Only"></param>
        /// <param name="skipWireless"></param>
        /// <param name="allowedMask"></param>
        /// <returns></returns>
        public static List<string> GetAllInterfaces(string allowedMask = "*",bool ipVersion4Only = true,bool skipWireless = false)
        {
            var result = new List<string>();

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // only “Up” interfaces
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                // skip the loopback device
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                // optional: skip Wi-Fi
                if (skipWireless &&
                    ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    continue;

                // if they only want IPv4, make sure this NIC actually supports it
                if (ipVersion4Only &&
                    !ni.Supports(NetworkInterfaceComponent.IPv4))
                    continue;

                var props = ni.GetIPProperties();

                // check each assigned address
                foreach (var uni in props.UnicastAddresses)
                {
                    // skip non-IPv4 if asked
                    if (ipVersion4Only &&
                        uni.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    // skip loopback
                    if (IPAddress.IsLoopback(uni.Address))
                        continue;

                    // optional mask filter
                    if (allowedMask != "*")
                    {
                        if (!IPAddress.TryParse(allowedMask, out var wantedMask) ||
                            !uni.IPv4Mask.Equals(wantedMask))
                        {
                            continue;
                        }
                    }

                    // ensure there is at least one gateway (optional)
                    if ((props.GatewayAddresses?.Count ?? 0) == 0 && allowedMask == "*")
                    {
                        // you can either skip or include—here we include
                        // continue;
                    }

                    result.Add(uni.Address.ToString());
                }
            }

            return result;
        }

        /// <summary>
        /// Get the System IP (For multi-network Static IP will be prefered by default)
        /// </summary>
        /// <returns></returns>
        public static string GetMyIp(bool preferStaticIP = true)
        {
            UnicastIPAddressInformation mostSuitableIp = null;

            foreach (NetworkInterface network in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                IPInterfaceProperties properties = network.GetIPProperties();

                if (properties.GatewayAddresses.Count == 0)
                {
                    continue;
                }

                foreach (UnicastIPAddressInformation address in properties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    if (IPAddress.IsLoopback(address.Address))
                    {
                        continue;
                    }

                    if (!address.IsDnsEligible)
                    {
                        if (mostSuitableIp == null)
                        {
                            mostSuitableIp = address;
                        }

                        continue;
                    }

                    //I know this logic is stupid, but its simple
                    if (preferStaticIP)
                    {
                        if (address.PrefixOrigin != PrefixOrigin.Dhcp)
                        {
                            if (mostSuitableIp == null || !mostSuitableIp.IsDnsEligible)
                            {
                                mostSuitableIp = address;
                            }

                            continue;
                        }
                    }
                    else
                    {
                        if (address.PrefixOrigin == PrefixOrigin.Dhcp)
                        {
                            if (mostSuitableIp == null || !mostSuitableIp.IsDnsEligible)
                            {
                                mostSuitableIp = address;
                            }

                            continue;
                        }
                    }

                    return address.Address.ToString();
                }
            }

            return mostSuitableIp != null
                ? mostSuitableIp.Address.ToString()
                : "";
        }

        private static string RunCommand(string command)
        {
            Process p = new();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = Environment.ExpandEnvironmentVariables("%SystemRoot%") + @"\System32\cmd.exe";
            p.StartInfo.Arguments = command;
            p.Start();
            // Read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }

        private static string RunCommandAdmin(string command)
        {
            Process p = new();
            p.StartInfo.UseShellExecute = true;
            //p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = Environment.ExpandEnvironmentVariables("%SystemRoot%") + @"\System32\cmd.exe";
            p.StartInfo.Arguments = command;
            p.StartInfo.Verb = "runas";
            p.Start();
            // Read the output stream first and then wait.
            // string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return null;
        }
    }
}