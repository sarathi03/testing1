using System;

namespace testing1.Models
{
    public class EthernetSettings
    {
        public bool IsStatic { get; set; } = false;
        public string IP { get; set; } = "";
        public string Gateway { get; set; } = "";
        public string Netmask { get; set; } = "";
        public string DnsMain { get; set; } = "";
        public string DnsBackup { get; set; } = "";
        public ushort Port { get; set; } = 502;
    }
} 