using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace testing1.Helper
{
    public static class PortScannerHelper
    {
        public static async Task<List<int>> ScanOpenPortsAsync(string ip, List<int> portsToScan, int timeout = 1000)
        {
            List<int> openPorts = new();

            foreach (int port in portsToScan)
            {
                using TcpClient tcpClient = new();
                try
                {
                    var task = tcpClient.ConnectAsync(ip, port);
                    if (await Task.WhenAny(task, Task.Delay(timeout)) == task && tcpClient.Connected)
                    {
                        openPorts.Add(port);
                    }
                }
                catch
                {
                    // Port is closed or unreachable
                }
            }

            return openPorts;
        }
    }
}
