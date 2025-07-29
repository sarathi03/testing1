using System;

namespace testing1.Helpers
{
    public class TcpClientHelper
    {
        private string _ip;
        public TcpClientHelper(string ip)
        {
            _ip = ip;
        }

        public bool Connect(string ip) => true;
        public void Disconnect() { }
        public void SendCommand(string command, byte[] data = null) { }
        public byte[] ReadResponse(int length) => new byte[length];
    }
} 