using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SorterControl.Config
{
    class ConnectSettig
    {
        public string Device_Id { get; set; }
        public string ConnectionType { get; set; }
        public string IpAdress { get; set; }
        public int Port { get; set; }
        public int Timeout { get; set; }
        public string PortName { get; set; }
        public string BaudRate { get; set; }
        public string ParityBit { get; set; }
        public int DataBits { get; set; }
        public string StopBit { get; set; }
    }
}
