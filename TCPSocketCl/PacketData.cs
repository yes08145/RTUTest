using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPSocketCl
{
    class PacketData
    {
        string sof = string.Empty;
        string u_sys_Device_ID = string.Empty;
        string length = string.Empty;
        string sensor_ID = string.Empty;
        string ch_Setting = string.Empty;
        string response_Channel = string.Empty;
        string data = string.Empty;
        string check_Sum = string.Empty;
        string eof = string.Empty;

        public string Sof { get => sof; set => sof = value; }
        public string U_sys_Device_ID { get => u_sys_Device_ID; set => u_sys_Device_ID = value; }
        public string Length { get => length; set => length = value; }
        public string Sensor_ID { get => sensor_ID; set => sensor_ID = value; }
        public string Ch_Setting { get => ch_Setting; set => ch_Setting = value; }
        public string Response_Channel { get => response_Channel; set => response_Channel = value; }
        public string Data { get => data; set => data = value; }
        public string Check_Sum { get => check_Sum; set => check_Sum = value; }
        public string Eof { get => eof; set => eof = value; }
    }
}
