using System;

namespace testing1.Models
{
    public class RS485Settings
    {
        public uint BaudRate { get; set; } = 9600;
        public byte Parity { get; set; } = 0;
        public byte DataBit { get; set; } = 8;
        public byte StopBit { get; set; } = 1;
    }
} 