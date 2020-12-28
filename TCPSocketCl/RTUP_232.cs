using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPSocketCl
{
    class RTUP_232<T>
    {
        public T sof { get; set; }
        public T usys_device_ID { get; set; }
        public T length { get; set; }
        public T sensor_ID { get; set; }
        public T packet_mode { get; set; }
        public T frame_header { get; set; }
        public T module_address { get; set; }
        public T command_length { get; set; }
        public T command { get; set; }
        public T relay_1 { get; set; }
        public T relay_2 { get; set; }
        public T relay_3 { get; set; }
        public T relay_4 { get; set; }
        public T[] check_sum = new T[2];

        public RTUP_232()
        {

        }
        public RTUP_232(T sof, T usys_device_ID, T length, T sensor_ID, T packet_mode, T frame_header,
            T module_address, T command_length, T command, T relay_1, T relay_2, T relay_3, T relay_4)
        {
            this.sof = sof;
            this.usys_device_ID = usys_device_ID;
            this.length = length;
            this.sensor_ID = sensor_ID;
            this.packet_mode = packet_mode;
            this.frame_header = frame_header;
            this.module_address = module_address;
            this.command_length = command_length;
            this.command = command;
            this.relay_1 = relay_1;
            this.relay_2 = relay_2;
            this.relay_3 = relay_3;
            this.relay_4 = relay_4;
        }
    }
}
