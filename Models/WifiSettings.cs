using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testing1.Models
{
    public class WifiSettings
    {
        public string SSID { get; set; }
        public string Password { get; set; }
        public bool Is_static { get; set; }
        public string IP { get; set; }
        public string Gateway { get; set; }
        public string netmask { get; set; }
        public string DNS_main { get; set; }
        public string DNS_backup { get; set; }
    }
}
