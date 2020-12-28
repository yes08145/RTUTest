using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MetroFramework.Forms;
namespace TCPSocketCl
{
    public partial class Form1 : Form
    {
        //bool conn = false;
        public static bool clickFlag = false;
        private static int IP1 = 0;
        private static int IP2 = 0;
        private static int IP3 = 0;
        private static int IP4 = 0;
        private static int PORT = 5000;
        private static int data = 0;
        private static int aout_ch = 0;
        private static int ain_ch = 0;
        private static int dout_ch = 0;
        private static int sensorID = 0;
        private static string IP = string.Empty;
        private static string q_ip1 = string.Empty;
        private static string q_ip2 = string.Empty;
        private static string q_ip3 = string.Empty;
        private static string q_ip4 = string.Empty;
        private static string q_port = string.Empty;
        private static string strHex = string.Empty;
        private static bool r_log_text = true;
        private static bool r_log_realBuff = false;
        private static bool r_log_splitBuff = false;
        private static bool s_log_text = true;
        private static bool s_log_sendBuff = false;
        //private static string strHexSplit = string.Empty;
        //private static string hex_cksum = string.Empty;
        private static string[] device_judge = new string[3];
        private static string[] logMsg = new string[6] { "전류 출력 설정", "전류 입력 값 요청", "디지털 출력 설정", "전류 출력 응답", "전류 입력 값 응답","디지털 입력 수신" };
        private static string DirPath = Directory.GetCurrentDirectory() + @"\Logs";
        //private static string filePath = Directory.GetCurrentDirectory() + @"\Logs\" + DateTime.Today.ToString("yyyyMMdd") + ".log";
        private static string filePath = DirPath+ @"\" + DateTime.Today.ToString("yyyyMMdd") + ".log";
        // (1) 소켓 객체 생성
        private static List<SocketInfo> socketInfo = new List<SocketInfo>();
        public delegate void FocusDelegate();
        public delegate void SocketDelegate(object obj);
        public delegate void LogDelegate(string msg);
        //public static SocketClass socketClass = null;
        private static List<string> ipList = new List<string>();
        private static List<RTUP_Modbus<string>> saved_protocal = new List<RTUP_Modbus<string>>();
        private static List<RTUP_232<string>> saved_protocal_232 = new List<RTUP_232<string>>();


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Log("Socket Client Program Start");
            //Log2("192.168.0.180:5000");
            //Log2("192.168.0.244:5000");
            //Log2("192.168.0.31:4265");
            device_judge[0] = "Device 오류";
            device_judge[1] = "RTU";
            device_judge[2] = "SmartPoE";
            
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            comboBox1.SelectedIndex = 0;
            //this.dataGridView1.RowHeadersDefaultCellStyle.BackColor = System.Drawing.SystemColors.ScrollBar;
            this.ActiveControl = textBox_IP1;
            Dgv_protocol_create();
            Dgv_Protocol_RS232_Create();
        }

        private void btn_connect_Click(object sender, EventArgs e)
        {
            try
            {
                TboxValue();
            }
            catch(FormatException ex)
            {
                MessageBox.Show("IP, PORT를 입력해주세요");
                return;
            }
            int socketCount = socketInfo.Count;
            IP = IP1 + "." + IP2 + "." + IP3 + "." + IP4;
            SocketConnect(IP,PORT);
            ListboxFocus();
            TboxClear();
            String resultIP = "IP:"+IP+", PORT:"+PORT;
            if (socketCount < socketInfo.Count)
            {
                StartThread(socketInfo[socketInfo.Count-1], Recv, "recv");
            }
            
        }

        private void btn_disconnect_Click(object sender, EventArgs e)
        {
            SocketDisconnect();
        }
        private void ListboxFocus()
        {
            listbox1.SelectedIndex = listbox1.Items.Count - 1;
        }

        public void Log(string msg)
        {
            //SocketClass sc = new SocketClass();
            string return_msg = string.Format("[{0}] {1}", DateTime.Now.ToString(), msg);
            listbox1.Items.Add(return_msg);
            //......
            //sc.LogFile(return_msg);       
            //DirectoryInfo di = new DirectoryInfo(DirPath);
            //FileInfo fi = new FileInfo(filePath);

            //try
            //{
            //    if (di.Exists != true) Directory.CreateDirectory(DirPath);
            //    if (fi.Exists != true)
            //    {
            //        using (StreamWriter sw = new StreamWriter(filePath))
            //        {
            //            sw.WriteLine(return_msg);
            //            sw.Close();
            //        }
            //    }
            //    else
            //    {
            //        using (StreamWriter sw = File.AppendText(filePath))
            //        {
            //            sw.WriteLine(return_msg);
            //            sw.Close();
            //        }
            //    }
            //}
            //catch(Exception e)
            //{
                
            //}

        }

        public void Log2(string msg)
        {
            //listBox_quick.Items.Add(string.Format(

            //"{0}", msg

            //));
            ipList.Add(msg);
            dgv_constate.DataSource = ipList.Select(ip => new { Value = ip }).ToList();
        }

        private void ButtonOSetting_Click(object sender, EventArgs e)
        {
            sensorID = 1;
            if (button1.BackColor == Color.LightBlue) aout_ch = 0;
            else if (button2.BackColor == Color.LightBlue) aout_ch = 1;
            //else aout_ch = 404;
            else sensorID = 100;
            try
            {
                data = Convert.ToInt32(comboBox1.Text);
                foreach (SocketInfo usedSockInfo in socketInfo)
                {
                    if (usedSockInfo.index == dgv_constate.SelectedRows[0].Index)//Convert.ToInt32(listBox_quick.SelectedItem.ToString().Split(')')[0])-1)
                    {
                        if (usedSockInfo.conn)
                        {
                            StartThread(usedSockInfo, Send, "send");
                        }
                    }
                }
            }
            catch(NullReferenceException ex)
            {
                MessageBox.Show("설정 값을 보낼 서버를 선택해주십시오.");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                MessageBox.Show("설정 값을 보낼 서버를 선택해주십시오.");
            }

        }
        private void Button_AIRequest_Click(object sender, EventArgs e)
        {
            sensorID = 2;
            if (button3.BackColor == Color.LightBlue) ain_ch = 0;
            else if (button4.BackColor == Color.LightBlue) ain_ch = 1;
            //else ain_ch = 404;
            else sensorID = 100;
            try
            {
                foreach (SocketInfo usedSockInfo in socketInfo)
                {
                    if (usedSockInfo.index == dgv_constate.SelectedRows[0].Index)
                    {
                        if (usedSockInfo.conn)
                        {
                            StartThread(usedSockInfo, Send, "send");
                        }
                    }
                }
            }
            catch(NullReferenceException ex)
            {
                MessageBox.Show("설정 값을 보낼 서버를 선택해주십시오.");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                MessageBox.Show("설정 값을 보낼 서버를 선택해주십시오.");
            }

        }
        private void Button_DOSetting_Click(object sender, EventArgs e)
        {
            sensorID = 4;
            if (button_DO0.BackColor == Color.LightBlue) dout_ch = 0;
            else if (button_DO1.BackColor == Color.LightBlue) dout_ch = 1;
            else if (button_DO2.BackColor == Color.LightBlue) dout_ch = 2;
            else if (button_DO3.BackColor == Color.LightBlue) dout_ch = 3;
            //else dout_ch = 404;
            else sensorID = 100;

            if (button_Off.BackColor == Color.LightBlue) data = 0;
            else if (button_On.BackColor == Color.LightBlue) data = 1;
            //else data = 404;
            else sensorID = 100;

            try
            {
                foreach (SocketInfo usedSockInfo in socketInfo)
                {
                    if (usedSockInfo.index == dgv_constate.SelectedRows[0].Index)
                    {
                        if (usedSockInfo.conn)
                        {
                            StartThread(usedSockInfo, Send, "send");
                        }
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show("설정 값을 보낼 서버를 선택해주십시오.");
            }
            catch(ArgumentOutOfRangeException ex)
            {
                MessageBox.Show("설정 값을 보낼 서버를 선택해주십시오.");
            }
        }
        private void Button_State(System.Windows.Forms.Button button, int state)
        {
            int btn_color = 1;
            if (button.BackColor == Color.LightBlue)
            {
                btn_color = 0;
            }
            if (state == 1)
            {
                button1.BackColor = System.Drawing.Color.Transparent;
                button2.BackColor = System.Drawing.Color.Transparent;
                this.ActiveControl = button_AOSetting;
            }
            else if (state == 2)
            {
                button3.BackColor = System.Drawing.Color.Transparent;
                button4.BackColor = System.Drawing.Color.Transparent;
                this.ActiveControl = button_AIRequest;
            }
            else if (state == 3)
            {
                button_DO0.BackColor = System.Drawing.Color.Transparent;
                button_DO1.BackColor = System.Drawing.Color.Transparent;
                button_DO2.BackColor = System.Drawing.Color.Transparent;
                button_DO3.BackColor = System.Drawing.Color.Transparent;
                this.ActiveControl = button_DOSetting;
            }
            else if(state == 4)
            {
                button_Off.BackColor = System.Drawing.Color.Transparent;
                button_On.BackColor = System.Drawing.Color.Transparent;
                this.ActiveControl = button_DOSetting;
            }
            if(btn_color == 1)
            {
                button.BackColor = Color.LightBlue;
            }
            
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Button_State(button1, 1);
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            Button_State(button2, 1);
        }
        private void Button3_Click(object sender, EventArgs e)
        {
            Button_State(button3,2);
        }
        private void Button4_Click(object sender, EventArgs e)
        {
            Button_State(button4,2);
        }
        private void Button_DO0_Click(object sender, EventArgs e)
        {
            Button_State(button_DO0, 3);
        }
        private void Button_DO1_Click(object sender, EventArgs e)
        {
            Button_State(button_DO1, 3);
        }
        private void Button_DO2_Click(object sender, EventArgs e)
        {
            Button_State(button_DO2, 3);
        }
        private void Button_DO3_Click(object sender, EventArgs e)
        {
            Button_State(button_DO3, 3);
        }
        private void Button_Off_Click(object sender, EventArgs e)
        {
            Button_State(button_Off, 4);
        }
        private void Button_On_Click(object sender, EventArgs e)
        {
            Button_State(button_On, 4);
        }
        
        private void tabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            Font fntTab;
            Brush bshBack;
            Brush bshFore;
            if (e.Index == tabControl.SelectedIndex)
            {
                fntTab = new Font(e.Font, FontStyle.Bold);
                bshBack = new System.Drawing.Drawing2D.LinearGradientBrush(e.Bounds, SystemColors.Control, SystemColors.Control, System.Drawing.Drawing2D.LinearGradientMode.BackwardDiagonal);
                bshFore = Brushes.Black;
            }
            else
            {
                fntTab = e.Font;
                bshBack = new SolidBrush(SystemColors.Control);
                bshFore = new SolidBrush(Color.Black);
            }
            string tabName = this.tabControl.TabPages[e.Index].Text;
            StringFormat sftTab = new StringFormat(StringFormatFlags.NoClip);        
            sftTab.Alignment = StringAlignment.Center;
            sftTab.LineAlignment = StringAlignment.Center;
            e.Graphics.FillRectangle(bshBack, e.Bounds);
            Rectangle recTab = e.Bounds;
            recTab = new Rectangle(recTab.X, recTab.Y + 4, recTab.Width, recTab.Height - 4);
            e.Graphics.DrawString(tabName, fntTab, bshFore, recTab, sftTab);
        }

        private void check_text_CheckedChanged(object sender, EventArgs e)
        {
            if (check_rtext.Checked == true) r_log_text = true;
            else r_log_text = false;
        }

        //private void check_realbuf_CheckedChanged(object sender, EventArgs e)
        //{
        //    if (check_realbuf.Checked == true) r_log_realBuff = true;
        //    else r_log_realBuff = false;
        //}

        private void check_splitbuf_CheckedChanged(object sender, EventArgs e)
        {
            if (check_splitbuf.Checked == true) r_log_splitBuff = true;
            else r_log_splitBuff = false;
        }

        private void check_stext_CheckedChanged(object sender, EventArgs e)
        {
            if (check_stext.Checked == true) s_log_text = true;
            else s_log_text = false;
        }

        private void check_sendbuf_CheckedChanged(object sender, EventArgs e)
        {
            if (check_sendbuf.Checked == true) s_log_sendBuff = true;
            else s_log_sendBuff = false;
        }

        private void dgv_constate_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(dgv_constate.RowHeadersDefaultCellStyle.ForeColor))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Near;
                e.Graphics.DrawString((e.RowIndex + 1).ToString(), e.InheritedRowStyle.Font, b, e.RowBounds.Location.X + 1, e.RowBounds.Location.Y + 3, sf);
            }
        }

        private void dgv_constate_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            this.dgv_constate.Rows[e.RowIndex].Selected = !(this.dgv_constate.Rows[e.RowIndex].Selected);
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            listbox1.Items.Clear();
        }
        private void Dgv_protocol_create()
        {
            RTUP_Modbus<string> rtup_modbus = new RTUP_Modbus<string>("02", "74", "0D", "05", "00", "01", "03", "01", "F4", "00", "02");
           
            byte[] target = new byte[6];
            target[0] = Convert.ToByte(rtup_modbus.slave_addr,16);
            target[1] = Convert.ToByte(rtup_modbus.func,16);
            target[2] = Convert.ToByte(rtup_modbus.start_addrH,16);
            target[3] = Convert.ToByte(rtup_modbus.start_addrL,16);
            target[4] = Convert.ToByte(rtup_modbus.length_H,16);
            target[5] = Convert.ToByte(rtup_modbus.length_L,16);

            //rtup_modbus.crc = TModbusRTU.MakeCRC16_byte(target, 6);
            saved_protocal.Add(rtup_modbus);
            /*
            dgv_protocol.AutoGenerateColumns = false;
            DataGridViewTextBoxColumn makeColumn = new DataGridViewTextBoxColumn();
            makeColumn.DataPropertyName = "sof";
            makeColumn.HeaderText = "SOF";
            makeColumn.DefaultCellStyle.Format = "X02";
            dgv_protocol.Columns.Add(makeColumn);
            makeColumn = new DataGridViewTextBoxColumn();
            makeColumn.DataPropertyName = "usys_device_ID";
            makeColumn.HeaderText = "U-Sys Device ID";
            makeColumn.DefaultCellStyle.Format = "X02";
            dgv_protocol.Columns.Add(makeColumn);
            makeColumn = new DataGridViewTextBoxColumn();
            makeColumn.DataPropertyName = "length";
            makeColumn.HeaderText = "Length";
            makeColumn.DefaultCellStyle.Format = "X02";
            dgv_protocol.Columns.Add(makeColumn);
            makeColumn = new DataGridViewTextBoxColumn();
            makeColumn.DataPropertyName = "sensor_ID";
            makeColumn.HeaderText = "Sensor ID";
            makeColumn.DefaultCellStyle.Format = "X02";
            dgv_protocol.Columns.Add(makeColumn);
            makeColumn = new DataGridViewTextBoxColumn();
            makeColumn.DataPropertyName = "packet_mode";
            makeColumn.HeaderText = "Packet 구분";
            makeColumn.DefaultCellStyle.Format = "X02";
            dgv_protocol.Columns.Add(makeColumn);
            makeColumn = new DataGridViewTextBoxColumn();
            makeColumn.DataPropertyName = "slave_addr";
            makeColumn.HeaderText = "Slave Address";
            makeColumn.DefaultCellStyle.Format = "X02";
            dgv_protocol.Columns.Add(makeColumn);
            makeColumn = new DataGridViewTextBoxColumn();
            makeColumn.DataPropertyName = "func";
            makeColumn.HeaderText = "Function";
            makeColumn.DefaultCellStyle.Format = "X02";
            dgv_protocol.Columns.Add(makeColumn);
            makeColumn = new DataGridViewTextBoxColumn();
            makeColumn.DataPropertyName = "start_addrH";
            makeColumn.HeaderText = "Start Address (HIGH)";
            makeColumn.DefaultCellStyle.Format = "X02";
            dgv_protocol.Columns.Add(makeColumn);
            makeColumn = new DataGridViewTextBoxColumn();
            makeColumn.DataPropertyName = "start_addrL";
            makeColumn.HeaderText = "Start Address (LOW)";
            makeColumn.DefaultCellStyle.Format = "X02";
            dgv_protocol.Columns.Add(makeColumn);
            makeColumn = new DataGridViewTextBoxColumn();
            makeColumn.DataPropertyName = "length_H";
            makeColumn.HeaderText = "Length(High)";
            makeColumn.DefaultCellStyle.Format = "X02";
            dgv_protocol.Columns.Add(makeColumn);
            makeColumn = new DataGridViewTextBoxColumn();
            makeColumn.DataPropertyName = "length_L";
            makeColumn.HeaderText = "Length(Low)";
            makeColumn.DefaultCellStyle.Format = "X02";
            dgv_protocol.Columns.Add(makeColumn);
            */

            dgv_protocol.DataSource = saved_protocal;
        }
        private void Dgv_Protocol_RS232_Create()
        {
            saved_protocal_232.Add(new RTUP_232<string>("02", "74", "0F", "06", "01", "24", "01", "06", "01", "02", "01", "01", "01"));
            saved_protocal_232.Add(new RTUP_232<string>("02", "74", "0F", "06", "01", "24", "01", "06", "01", "01", "02", "01", "01"));
            saved_protocal_232.Add(new RTUP_232<string>("02", "74", "0F", "06", "01", "24", "01", "06", "01", "01", "01", "02", "01"));
            saved_protocal_232.Add(new RTUP_232<string>("02", "74", "0F", "06", "01", "24", "01", "06", "01", "01", "01", "01", "02"));
            dgv_protocol_232.DataSource = saved_protocal_232;
        }

        private void Btn_send_rs485_Click(object sender, EventArgs e)
        {
            sensorID = 5;
            try
            {
                foreach (SocketInfo usedSockInfo in socketInfo)
                {
                    if (usedSockInfo.index == dgv_constate.SelectedRows[0].Index)
                    {
                        if (usedSockInfo.conn)
                        {
                            StartThread(usedSockInfo, Send, "send");
                        }
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show("설정 값을 보낼 서버를 선택해주십시오.");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                MessageBox.Show("설정 값을 보낼 서버를 선택해주십시오.");
            }
        }
        private void Btn_send_rs232_Click(object sender, EventArgs e)
        {
            sensorID = 6;
            try
            {
                foreach (SocketInfo usedSockInfo in socketInfo)
                {
                    if (usedSockInfo.index == dgv_constate.SelectedRows[0].Index)
                    {
                        if (usedSockInfo.conn)
                        {
                            StartThread(usedSockInfo, Send, "send");
                        }
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show("설정 값을 보낼 서버를 선택해주십시오.");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                MessageBox.Show("설정 값을 보낼 서버를 선택해주십시오.");
            }
        }
    }
}
