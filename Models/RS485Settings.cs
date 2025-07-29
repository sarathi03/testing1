using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testing1.Models
{
    public class RS485Settings
    {
        public uint BaudRate { get; set; }
        public byte Parity { get; set; }
        public byte DataBit { get; set; }
        public byte StopBit { get; set; }
    }
}
