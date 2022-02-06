using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GigeVision.Core
{
    public class NetworkService
    {
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
    }
}
