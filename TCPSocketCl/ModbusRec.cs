using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPSocketCl
{
    class ModbusRec
    {
        public byte sof = 0x02;
        public byte usys_device_ID = 0x73;
        public byte length;
        public byte sensor_ID;
        public byte packet;
        public byte s_address;
        public byte rs_function;
        public byte byte_count;
        public byte data1_h;
        public byte data1_l;
        public byte data2_h;
        public byte data2_l;
        public byte[] crc16 = new byte[2];
        
    }
}
