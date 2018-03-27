using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Distributed.Internal.Source.Util
{
    public static class IPUtil
    {
        public static string GetLocalIpAddress()
        {
            UnicastIPAddressInformation mostSuitableIp = null;

            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var network in networkInterfaces)
            {
                if (network.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                var properties = network.GetIPProperties();
                if (properties.GatewayAddresses.Count == 0)
                {
                    continue;
                }

                foreach (var address in properties.UnicastAddresses)
                {
                    var family = address.Address.AddressFamily;
                    if (family != AddressFamily.InterNetwork &&
                        family != AddressFamily.InterNetworkV6)
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

                    // The best IP is the IP got from DHCP server
                    if (address.PrefixOrigin != PrefixOrigin.Dhcp)
                    {
                        if (mostSuitableIp == null || !mostSuitableIp.IsDnsEligible)
                        {
                            mostSuitableIp = address;
                        }
                        continue;
                    }

                    return FormatAddress(address.Address);
                }
            }

            return FormatAddress(mostSuitableIp?.Address);
        }

        private static string FormatAddress(IPAddress ipAddress)
        {
            if (ipAddress == null)
            {
                return null;
            }

            if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                return ipAddress.ToString();
            }

            string address;
            if (ipAddress.IsIPv6LinkLocal)
            {
                address = ipAddress.ToString().Replace("::", ":").Replace(':', '-');
                address = address.Substring(0, address.IndexOf('%'));
            }
            else
            {
                address = ipAddress.ToString().Replace(':', '-');
                if (address.StartsWith("-"))
                {
                    address = "0" + address;
                }
            }

            return $"{address}.ipv6-literal.net";
        }
    }
}
