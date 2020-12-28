using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetroFramework.Controls;

namespace TCPSocketCl
{
    class RTUP_Modbus<T>
    {
        public T sof { get; set; }
        public T usys_device_ID { get; set; }
        public T length { get; set; }
        public T sensor_ID { get; set; }
        public T packet_mode { get; set; } // 0x00 Tx   0x01 Rx
        public T slave_addr { get; set; }
        public T func { get; set; }
        public T start_addrH { get; set; }
        public T start_addrL { get; set; }
        public T length_H { get; set; }
        public T length_L { get; set; }
        public T[] crc = new T[2];
        public RTUP_Modbus()
        {

        }
        public RTUP_Modbus(T sof,T usys_device_ID, T length, T sensor_ID, T packet_mode, T slave_addr, T func, T start_addrH, T start_addrL,T length_H, T length_L)
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
