using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetroFramework.Controls;

namespace TCPSocketCl
{
    class RTUP_Modbus
    {
        public byte sof { get; set; }
        public byte usys_device_ID { get; set; }
        public byte length { get; set; }
        public byte sensor_ID { get; set; }
        public byte packet_mode { get; set; } // 0x00 Tx   0x01 Rx
        public byte slave_addr { get; set; }
        public byte func { get; set; }
        public byte byte_count;
        public byte start_addrH { get; set; }
        public byte start_addrL { get; set; }
        public byte length_H { get; set; }
        public byte length_L { get; set; }
        public byte[] crc = new byte[2];
        public RTUP_Modbus()
        {

        }
        public RTUP_Modbus(byte sof,byte usys_device_ID, byte length, byte sensor_ID, byte packet_mode, byte slave_addr, byte func, byte start_addrH, byte start_addrL,byte length_H, byte length_L)
        {
            this.sof = sof;
            this.usys_device_ID = usys_device_ID;
            this.length = length;
            this.sensor_ID = sensor_ID;
            this.packet_mode = packet_mode;
            this.slave_addr = slave_addr;
            this.func = func;
            this.start_addrH = start_addrH;
            this.start_addrL = start_addrL;
            this.length_H = length_H;
            this.length_L = length_L;

        }
    }
}
