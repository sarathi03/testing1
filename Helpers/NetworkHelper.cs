using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System;

namespace testing1.Helper
{
    public static class NetworkHelper
    {
        public static List<(string ip, string subnet)> GetAllLocalSubnets()
        {
            var result = new List<(string ip, string subnet)>();

            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus == OperationalStatus.Up &&
                    adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    adapter.Supports(NetworkInterfaceComponent.IPv4))
                {
                    var ipProps = adapter.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string ip = addr.Address.ToString();
                            string subnet = addr.IPv4Mask?.ToString();
                            if (!string.IsNullOrEmpty(subnet))
                            {
                                result.Add((ip, subnet));
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static List<IPAddress> GetAllIPsInSubnet(string ipAddress, string subnetMask)
        {
            byte[] ip = IPAddress.Parse(ipAddress).GetAddressBytes();
            byte[] mask = IPAddress.Parse(subnetMask).GetAddressBytes();

            byte[] networkPrefix = new byte[4];
            for (int i = 0; i < 4; i++)
                networkPrefix[i] = (byte)(ip[i] & mask[i]);

            uint start = BitConverter.ToUInt32(networkPrefix.Reverse().ToArray(), 0);
            uint maskBits = BitConverter.ToUInt32(mask.Reverse().ToArray(), 0);
            uint hostBits = ~maskBits;
            uint end = start + hostBits - 1;

            List<IPAddress> ips = new();
            for (uint i = start + 1; i < end; i++)
            {
                byte[] bytes = BitConverter.GetBytes(i).Reverse().ToArray();
                ips.Add(new IPAddress(bytes));
            }

            return ips;
        }

        public static string GetMacAddressFromIp(string ipAddress)
        {
            Process p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "arp",
                    Arguments = "-a",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            string pattern = $@"{Regex.Escape(ipAddress)}\s+([a-f0-9\-:]{{17}}|[a-f0-9\-:]{{2}}([\-:]?[a-f0-9]{{2}}){{5}})";
            var match = Regex.Match(output, pattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : "MAC address not found (try pinging first)";
        }
    }
}
