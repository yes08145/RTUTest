using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPSocketCl
{
    class RTUP
    {
        //POE 응답대기? 신호 02-73-07-0E-00-8A-03
        //public byte sof = 0x02;
        //public byte usys_device_ID = 0x74;
        //public byte length = 0x09;
        //public byte sensor_ID;
        //public byte ch_setting;
        //public byte data;
        //public byte[] check_sum = new byte[2];
        //public byte eof = 0x03;
        //public byte response_channel;
        public byte sof = 0x02;
        public byte usys_device_ID = 0x73;
        public byte length = 0x09;
        public byte sensor_ID;
        public byte ch_setting;
        public byte data;
        public byte[] check_sum = new byte[2];
        public byte eof = 0x03;
        public byte response_channel;

    }
}
