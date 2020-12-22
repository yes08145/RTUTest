using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace TCPSocketCl
{
    public partial class Form1 : Form
    {
        private static Mutex mutex = new Mutex();
        private static bool IsConnected(Socket socket)
        {
            return socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0;
        }
        private void SocketConnect(string in_IP, int in_PORT)
        {
            try
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(in_IP), in_PORT);
                Log("=======" + in_IP + ":" + in_PORT + " Connect 시도중=======");
                try
                {
                    Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    sock.Connect(ep);
                    socketInfo.Add(new SocketInfo(sock, in_IP, in_PORT, true,socketInfo.Count)); // 0부터 시작 인덱스
                    Log2(in_IP + ":" + in_PORT); // 1이 시작 인덱스
                    Log("========= IP: " + in_IP + ", PORT: " + in_PORT + " Connect 완료 =========");
                }
                catch
                {
                    Log("=======Connect Fail=======");
                    MessageBox.Show("서버에 연결할 수 없습니다.");
                }

            }
            catch(FormatException e)
            {
                MessageBox.Show("IP주소의 형식이 잘못됐습니다.");
            }
            catch (Exception e)
            {
                if (e.Message == "이미 연결되어있음")
                {
                    MessageBox.Show(e.Message);
                }
                return;
            }
        }
        private void SocketDisconnect()
        {
            try
            {
                foreach (SocketInfo usedSockInfo in socketInfo)
                {
                    if (usedSockInfo.index == dgv_constate.SelectedRows[0].Index)
                    {
                        SocketDisconnect(usedSockInfo);
                        return;
                    }
                }
                MessageBox.Show("Connect중인 서버가 없습니다.");
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show("Disconnect할 서버를 선택해주십시오.");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                MessageBox.Show("Disconnect할 서버를 선택해주십시오.");
            }
        }
        private void SocketDisconnect(object obj)
        {
            try
            {
                if (obj.GetType() != typeof(SocketInfo))
                {
                    throw new Exception("Send(object obj): obj 타입이 SocketInfo가 아님");
                }
                else
                {
                    SocketInfo throwSockInfo = (SocketInfo)obj;
                    if (throwSockInfo.conn)
                    {
                        throwSockInfo.conn = false;
                        throwSockInfo.sock.Shutdown(SocketShutdown.Both);
                        throwSockInfo.sock.Close();
                        ipList.RemoveAt(throwSockInfo.index);
                        socketInfo.RemoveAt(throwSockInfo.index);
                        int i = 0;
                        foreach(SocketInfo list in socketInfo)
                        {

                            //string listBox_text = listBox_quick.Items[i].ToString().Split(')')[1];
                            list.index = i;

                            //listBox_quick.Items[i] = (list.index+1)+")"+listBox_text;
                            i++;
                        }
                        dgv_constate.DataSource = ipList.Select(ip => new { Value = ip }).ToList();
                        Log("======= Connect 종료 =======");
                        ListboxFocus();
                    }
                }
            }
            catch(Exception e)

            {
                Log("======= 비정상 Connect 종료 =======");
                ListboxFocus();
            }
        }
        private void StartThread(SocketInfo socketInfo, SocketDelegate socketDelegate, string str)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(socketDelegate));
            if (str == "recv") thread.Priority = ThreadPriority.Highest;
            else if(str == "send") thread.Priority = ThreadPriority.Lowest;
            thread.Start(socketInfo);
        }
        public void Send(object obj)
        {
            try
            {
                if(obj.GetType() != typeof(SocketInfo))
                {
                    throw new Exception("Send(object obj): obj 타입이 SocketInfo가 아님");
                }
                else
                {
                    byte[] sendBuff = MakeMsg((SocketInfo)obj);
                    SocketInfo socketInfo = (SocketInfo)obj;
                    socketInfo.sock.Send(sendBuff);
                    strHex = BitConverter.ToString(sendBuff);
                    Queue<byte> sendQueue = new Queue<byte>(sendBuff);
                    ResultSet result = SplitAndCksum(socketInfo,sendQueue);
                    string hex_cksum = result.hex_cksum;
                    string log_result = JudgeAction(strHex, hex_cksum, socketInfo);
                    socketInfo.r_Buff = null;
                    //# D I/O 추가에 따라 기존 log 텍스트 불러오는 공식이 깨짐
                    //# 이를 해결하기 위한 배열 위치조정
                    //# 이로인한 sensorID 손상 인지요망

                    if (!InvokeRequired)
                    {
                        Log(log_result);
                        Log(strHex);
                        ListboxFocus();
                    }
                    else
                    {
                        this.Invoke(new Action(() =>
                        {
                            if (s_log_text) Log(log_result);
                            if (s_log_sendBuff) Log("SendBuffer   : " + strHex);
                        }
                        ));
                        //this.Invoke(new LogDelegate(Log), log_result);
                        //this.Invoke(new LogDelegate(Log), strHex);
                        this.Invoke(new FocusDelegate(ListboxFocus));
                    }

                }
                
            }
            //catch(FormatException ee)
            //{
            //    MessageBox.Show("채널 또는 값을 선택해주세요");
            //    //throw ee;
            //}
            catch(Exception e)
            {
                //MessageBox.Show("Function Send Exception Check");
            }
        }
        public byte[] CheckQueue(SocketInfo socketInfo, Queue<byte> recvBuff)
        {
            // buffer의 FF값을 최초 초기화값으로 정의한다.
            // 이와 소켓 recv 패킷과 값 충돌이 나는 경우는 device 장비가 236대 이상일 경우 발생한다.
            // 이 경우에 체크섬 결과 값이 FF가 나오기 때문이다.
            // 테스트 용임을 감안하여 이에 대해서 고려하지않는다.
            // 이를 해결하기 위해서는 SocketEvent 클래스 구조를 전체적으로 수정해야한다.
            byte[] receiverBuff = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                receiverBuff[i] = 0xFF;
            }
            if (recvBuff.Count == 0)
            {

                int result = socketInfo.sock.Receive(receiverBuff);
                if(result == 0)
                {
                    throw new SocketException();
                }
                for (int i = 0; i < receiverBuff.Length; i++)
                {
                    recvBuff.Enqueue(receiverBuff[i]);
                }
                this.Invoke(new Action(() => { if (r_log_realBuff) Log("ReceiveBuff : " + BitConverter.ToString(receiverBuff)); }));
                this.Invoke(new FocusDelegate(ListboxFocus));
            }
            else if(recvBuff.Peek() == 0xFF)
            {
                recvBuff.Clear();
                int result = socketInfo.sock.Receive(receiverBuff);
                if(result == 0)
                {
                    throw new SocketException();
                }
                for (int i = 0; i < receiverBuff.Length; i++)
                {
                    recvBuff.Enqueue(receiverBuff[i]);
                }
                this.Invoke(new Action(() => { if (r_log_realBuff) Log("ReceiveBuff : " + BitConverter.ToString(receiverBuff)); }));
                this.Invoke(new FocusDelegate(ListboxFocus));
            }
            return receiverBuff;
        }
        public void Recv(object obj)
        {
            try
            {
                if (obj.GetType() != typeof(SocketInfo))
                {
                    throw new Exception("Recv(object obj): obj 타입이 SocketInfo가 아님");
                }
                else
                {
                    SocketInfo socketInfo = (SocketInfo)obj;
                    Queue<byte> recvBuff = new Queue<byte>(1024);
                    while (socketInfo.conn)
                    {
                        byte[] receiverBuff = CheckQueue(socketInfo, recvBuff);
                        //if(IsConnected(socketInfo.sock))
                        //{
                        //    throw new Exception("Server Disconnect");
                        //}
                        // Splitcksum return을 int resultSet 에서 
                        // (strHexSplit, hex_cksum, resultSet)을 가지는 class ResultSet 으로 바꿈
                        // 전역변수 지역변수화
                        string hex_cksum = string.Empty;
                        string strHexSplit = string.Empty;
                        string log_result = string.Empty;
                        int resultSet = 0;
                        try
                        {
                            ResultSet result = SplitAndCksum(socketInfo, recvBuff);
                            if (result.strHexSplit == string.Empty) continue; //값을 빠른속도로 받아올 때 오류발생해서 추가(10-22)
                            strHexSplit = result.strHexSplit;
                            hex_cksum = result.hex_cksum;
                            resultSet = result.resultSet;
                            // 테스트 결과 send가 recv log앞에 있어서 출력창에 recv-send순으로 log가 뜨지않고
                            // send-recv순으로 뜸
                            // 따라서 JudgeAction 순서 앞으로 변경
                            log_result = JudgeAction(strHexSplit, hex_cksum, socketInfo);
                        }
                        catch (IndexOutOfRangeException e)
                        {
                            resultSet = 7 ; // resultSet은 SensorID의 값이기 때문에 아래 if문에 걸리지 않게만 즉, resultSet을 0,3이 아닌 다른값으로 지정하면 됨
                            log_result = "Index 오류 발생";
                        }
                        catch(Exception e)
                        {
                            if (e.Message == "continue")
                            {
                                continue;
                            }
                        }
                        if (resultSet == 0) continue;
                        //else if (resultSet == 3)
                        //{
                        //    if (socketInfo.conn)
                        //    {
                        //        // sensorID가 3에서 4로 변하면서 cksum이 1증가함
                        //        //hex_cksum = (Convert.ToInt32(hex_cksum) + 1).ToString();
                                    
                        //            StartThread(socketInfo, Send, "send");
                                    
                        //        //recv log에 띄울 cksum으로 다시 되돌림
                        //        //hex_cksum = (Convert.ToInt32(hex_cksum) - 1).ToString();
                        //    }
                        //}

                        if (!InvokeRequired)
                        {
                            Log(log_result);
                            Log(strHexSplit);
                            ListboxFocus();
                        }
                        else
                        {
                            this.Invoke(new Action(() =>
                            {
                                if (r_log_text) Log(log_result);
                                //if (r_log_realBuff) Log("ReceiveBuff : " + BitConverter.ToString(receiverBuff));
                                if (r_log_splitBuff) Log("SplitReceive : " + strHexSplit);
                            }
                            ));
                            this.Invoke(new FocusDelegate(ListboxFocus));
                        }
                        socketInfo.r_Buff = null;
                        //Thread.Sleep(1000);
                    }
                }

            }
            catch (Exception e)
            {
                if (e.Message == "Recv(object obj): obj 타입이 SocketInfo가 아님")
                {

                }
                else if (e.GetType().Name == "SocketException")
                {
                    SocketInfo disconnectedSocketInfo = (SocketInfo)obj;
                    //뮤텍스: 공유자원 reconnIP-재접속할 IP, reconnPORT-재접속할 PORT
                    //Lock
                    //mutex.WaitOne();
                    string reconnIP = disconnectedSocketInfo.IP;
                    int reconnPORT = disconnectedSocketInfo.PORT;
                    int reconnIndex = disconnectedSocketInfo.index;
                    if (disconnectedSocketInfo.conn)
                    {
                        this.Invoke(new SocketDelegate(SocketDisconnect), disconnectedSocketInfo);
                        this.Invoke(new Action(() =>
                        {
                            if (MessageBox.Show(this, (reconnIndex + 1) + "번 서버" + reconnIP + ":" + reconnPORT + "와의 연결이 끊겼습니다.\n다시 접속하시겠습니까?", "Error", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                //reconn = true;
                                int socketCount = socketInfo.Count;
                                SocketConnect(reconnIP, reconnPORT);
                                //ListboxFocus();
                                if (socketCount < socketInfo.Count)
                                {
                                    StartThread(socketInfo[socketInfo.Count - 1], Recv, "recv");
                                }
                            }
                            else
                            {
                                //reconn = false;
                                reconnIP = string.Empty;
                                reconnPORT = 0;
                            }
                        }
                        ));
                    }
                    //Unlock
                    //mutex.ReleaseMutex();

                }
                //else if (e.Message == "Server Disconnect")
                //{
                //    SocketInfo disconnectedSocketInfo = (SocketInfo)obj;
                //    string reconnIP = disconnectedSocketInfo.IP;
                //    int reconnPORT = disconnectedSocketInfo.PORT;
                //    int reconnIndex = disconnectedSocketInfo.index;
                //    if (disconnectedSocketInfo.conn)
                //    {
                //        this.Invoke(new SocketDelegate(SocketDisconnect), disconnectedSocketInfo);
                //        this.Invoke(new Action(() =>
                //        {
                //            MessageBox.Show((reconnIndex + 1) + "번 서버" + reconnIP + ":" + reconnPORT + " 와의 연결이 끊겼습니다.");
                //        }
                //        ));
                //    }
                //}
            }
        }

        public byte[] MakeMsg(SocketInfo sendSocketInfo)
        {
            byte[] msg;
            RTUP rtup = new RTUP();
            byte[] receiverBuff = sendSocketInfo.r_Buff;
            //if (receiverBuff != null)
            //{
            //    string r_strHex = BitConverter.ToString(receiverBuff);
            //    rtup.usys_device_ID = Convert.ToByte(Convert.ToInt32("0x"+r_strHex.Split('-')[1],16));
            //    rtup.length = Convert.ToByte(Convert.ToInt32(r_strHex.Split('-')[2],16));
            //    rtup.sensor_ID = Convert.ToByte(Convert.ToInt32(r_strHex.Split('-')[3],16));
            //    rtup.ch_setting = Convert.ToByte(Convert.ToInt32(r_strHex.Split('-')[4],16));
            //    rtup.data = Convert.ToByte(Convert.ToInt32(r_strHex.Split('-')[5], 16));
            //    rtup.check_sum[0] = Convert.ToByte(Convert.ToInt32(r_strHex.Split('-')[6], 16));
            //    rtup.check_sum[1] = Convert.ToByte(Convert.ToInt32(r_strHex.Split('-')[7], 16));
            //    sendSocketInfo.r_Buff = null;
            //}
            //else
            //{
                rtup.usys_device_ID = 0x74;
            // 2020-12-18 sensorID 추가로 인한 주석(테스트중)
                //rtup.sensor_ID = (byte)sensorID;
            //}
            //0 or 1 장비 선택

            
            try
            {
                // 2020-12-18 sensorID 추가로 인한 주석(테스트중)
                //if (rtup.sensor_ID == 1)
                if (sensorID == 1)
                {
                    // 2020-12-18 sensorID 추가
                    rtup.sensor_ID = (byte)sensorID;
                    //if(data == 404 || aout_ch == 404) throw new FormatException("채널 또는 data가 올바르지 않습니다.");
                    // 4~20mA
                    rtup.ch_setting = (byte)aout_ch;
                    rtup.data = (byte)data;
                    rtup.length = 0x09;
                    //checksum
                    int checkSum = rtup.sof + rtup.usys_device_ID + rtup.length + rtup.sensor_ID + rtup.ch_setting + rtup.data;

                    rtup.check_sum[0] = (byte)(checkSum / 256);
                    rtup.check_sum[1] = (byte)(checkSum % 256);

                    msg = new byte[9];
                    msg[0] = rtup.sof;
                    msg[1] = rtup.usys_device_ID;
                    msg[2] = rtup.length;
                    msg[3] = rtup.sensor_ID;
                    msg[4] = rtup.ch_setting;
                    msg[5] = rtup.data;
                    msg[6] = rtup.check_sum[0];
                    msg[7] = rtup.check_sum[1];
                    msg[8] = rtup.eof;
                }
                // 2020-12-18 sensorID 추가로 인한 주석(테스트중)
                //else if (rtup.sensor_ID == 2)
                else if(sensorID ==2)
                {
                    // 2020-12-18 sensorID 추가
                    rtup.sensor_ID = (byte)sensorID;
                    rtup.ch_setting = (byte)ain_ch;
                    rtup.length = 0x08;
                    int checkSum = rtup.sof + rtup.usys_device_ID + rtup.length + rtup.sensor_ID + rtup.ch_setting;
                    rtup.check_sum[0] = (byte)(checkSum / 256);
                    rtup.check_sum[1] = (byte)(checkSum % 256);

                    msg = new byte[8];
                    msg[0] = rtup.sof;
                    msg[1] = rtup.usys_device_ID;
                    msg[2] = rtup.length;
                    msg[3] = rtup.sensor_ID;
                    msg[4] = rtup.ch_setting;
                    msg[5] = rtup.check_sum[0];
                    msg[6] = rtup.check_sum[1];
                    msg[7] = rtup.eof;
                }
                // 2020-12-18 sensorID 추가로 인한 주석(테스트중)
                //else if (rtup.sensor_ID == 3)
                else if(sensorID == 3)
                {
                    // 2020-12-18 sensorID 추가
                    rtup.sensor_ID = (byte)sensorID;
                    int checkSum = rtup.check_sum[0] * 256 + rtup.check_sum[1]+1;
                    rtup.check_sum[0] = (byte)(checkSum / 256);
                    rtup.check_sum[1] = (byte)(checkSum % 256);
                    msg = new byte[9];
                    msg[0] = rtup.sof;
                    msg[1] = rtup.usys_device_ID;
                    msg[2] = rtup.length;
                    msg[3] = Convert.ToByte(rtup.sensor_ID+1);
                    msg[4] = rtup.ch_setting;
                    msg[5] = rtup.data;
                    msg[6] = rtup.check_sum[0];
                    msg[7] = rtup.check_sum[1];
                    msg[8] = rtup.eof;
                }
                // 2020-12-18 sensorID 추가로 인한 주석(테스트중)
                //else if (rtup.sensor_ID == 4)
                else if(sensorID == 4)
                {
                    // 2020-12-18 sensorID 추가
                    rtup.sensor_ID = (byte)sensorID;
                    // digit 0/1
                    rtup.ch_setting = (byte)dout_ch;
                    if(data != 0 && data != 1)
                    {
                        throw new FormatException("보내고자하는 Digital Output\t"+data+"은 잘못된 값입니다.");
                    }
                    rtup.data = (byte)data;
                    rtup.length = 0x09;
                    //checksum
                    int checkSum = rtup.sof + rtup.usys_device_ID + rtup.length + rtup.sensor_ID + rtup.ch_setting + rtup.data;
                    rtup.check_sum[0] = (byte)(checkSum / 256);
                    rtup.check_sum[1] = (byte)(checkSum % 256);

                    msg = new byte[9];
                    msg[0] = rtup.sof;
                    msg[1] = rtup.usys_device_ID;
                    msg[2] = rtup.length;
                    msg[3] = rtup.sensor_ID;
                    msg[4] = rtup.ch_setting;
                    msg[5] = rtup.data;
                    msg[6] = rtup.check_sum[0];
                    msg[7] = rtup.check_sum[1];
                    msg[8] = rtup.eof;
                }
                //rs-485
                else if(sensorID == 5)
                {
                    RTUP_Modbus rtup_modbus = new RTUP_Modbus(0x02, 0x74, 0x0D, 0x05, 0x00, 0x01, 0x03, 0x01, 0xF4, 0x00, 0x02);
                    byte[] target = new byte[6] {
                        rtup_modbus.slave_addr,
                        rtup_modbus.func,
                        rtup_modbus.start_addrH,
                        rtup_modbus.start_addrL,
                        rtup_modbus.length_H,
                        rtup_modbus.length_L
                    };

                    rtup_modbus.crc = TModbusRTU.MakeCRC16_byte(target, 6);
                    msg = new byte[13];
                    msg[0] = rtup_modbus.sof;
                    msg[1] = rtup_modbus.usys_device_ID;
                    msg[2] = rtup_modbus.length;
                    msg[3] = rtup_modbus.sensor_ID;
                    msg[4] = rtup_modbus.packet_mode;
                    msg[5] = rtup_modbus.slave_addr;
                    msg[6] = rtup_modbus.func;
                    msg[7] = rtup_modbus.start_addrH;
                    msg[8] = rtup_modbus.start_addrL;
                    msg[9] = rtup_modbus.length_H;
                    msg[10] = rtup_modbus.length_L;
                    msg[11] = rtup_modbus.crc[0];
                    msg[12] = rtup_modbus.crc[1];
                }
                else
                {
                    throw new NullReferenceException("잘못된 요청명령");
                }
            }
            catch (FormatException ee)
            {
                MessageBox.Show("Digitial Output data는 잘못된값입니다.");
                throw ee;
            }
            catch (NullReferenceException e)
            {
                MessageBox.Show("채널 또는 data가 올바르지 않습니다.");
                throw e;
            }
            return msg;
        }

        public string JudgeAction(string txt, string hex_cksum, SocketInfo socketInfo)
        {
            string log = string.Empty;
            byte sensor_data = Convert.ToByte(txt.Split('-')[3]);
            
            if (sensor_data == 5)
            {
                byte packet_data = Convert.ToByte(txt.Split('-')[4]);
                if (packet_data == 0)
                {
                    RTUP_Modbus rtup_m = new RTUP_Modbus();
                    rtup_m.usys_device_ID = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[1], 16));
                    rtup_m.length = Convert.ToByte(Convert.ToInt32(txt.Split('-')[2], 16));
                    /*rtup_m.sensor_ID = sensor_data;
                    rtup_m.packet_mode = Convert.ToByte(Convert.ToInt32(txt.Split('-')[4]));
                    rtup_m.slave_addr = Convert.ToByte(Convert.ToInt32(txt.Split('-')[5],16));
                    rtup_m.func = Convert.ToByte(Convert.ToInt32(txt.Split('-')[6], 16));
                    rtup_m.start_addrH = Convert.ToByte(Convert.ToInt32(txt.Split('-')[7], 16));
                    rtup_m.start_addrL = Convert.ToByte(Convert.ToInt32(txt.Split('-')[8], 16));
                    rtup_m.length_H = Convert.ToByte(Convert.ToInt32(txt.Split('-')[9], 16));
                    rtup_m.length_L = Convert.ToByte(Convert.ToInt32(txt.Split('-')[10], 16));
                    rtup_m.crc[0] = Convert.ToByte(Convert.ToInt32(txt.Split('-')[11], 16));
                    rtup_m.crc[1] = Convert.ToByte(Convert.ToInt32(txt.Split('-')[12], 16));
                    */
                    string device = socketInfo.IP + ":" + socketInfo.PORT;
                    int device_num = 0;
                    if (rtup_m.usys_device_ID == 0x74) device_num = 1;
                    else
                    {
                        log = device_judge[device_num];
                        return log;
                    }
                    if (rtup_m.length == 13) log = "Device '" + device + "'으로 RS-485 Modbus Data Tx Packet 전송";
                    else
                    {
                        log = "잘못된 크기로 인한 전송 실패";

                    }
                    return log;
                }
                else if (packet_data == 1)
                {
                    ModbusRec mod = new ModbusRec();

                    string device = socketInfo.IP + ":" + socketInfo.PORT;
                    mod.usys_device_ID = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[1], 16));
                    mod.length = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[2], 16));

                    mod.s_address = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[5], 16));
                    mod.rs_function = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[6], 16));
                    mod.byte_count = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[7], 16));
                    mod.data1_h = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[8], 16));
                    mod.data1_l = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[9], 16));
                    mod.data2_h = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[10], 16));
                    mod.data2_l = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[11], 16));
                    mod.crc16[0] = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[12], 16));
                    mod.crc16[1] = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[13], 16));

                    byte[] crc_sum = new byte[] { mod.s_address, mod.rs_function, mod.byte_count, mod.data1_h, mod.data1_l, mod.data2_h, mod.data2_l };
                    //string device = socketInfo.IP + ":" + socketInfo.PORT;

                    int device_num = 0;
                    if (mod.usys_device_ID == 0x74) device_num = 1;


                    byte[] ret = TModbusRTU.MakeCRC16_byte(crc_sum, 7);
                    if ((ret[0] == mod.crc16[0]) && (ret[1] == mod.crc16[1]))
                    {
                        log = "CRC16통과";
                    }
                    else
                    {
                        log = "CRC Check 오류";
                    }
                    return log;
                }
                else return ""; 
            }
            else
            {
                RTUP rtup = new RTUP();

                rtup.usys_device_ID = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[1], 16));
                rtup.length = Convert.ToByte(Convert.ToInt32(txt.Split('-')[2], 16));
                rtup.sensor_ID = Convert.ToByte(txt.Split('-')[3]);
                rtup.response_channel = Convert.ToByte(txt.Split('-')[4]);
                rtup.data = Convert.ToByte(Convert.ToInt32(txt.Split('-')[5], 16)); // 나중에 data값으로 문제가 생기면 if안의 지역으로 위치수정
                string device = socketInfo.IP + ":" + socketInfo.PORT;
                string start_cksum = string.Empty;
                string last_cksum = string.Empty;
                int device_num = 0;
                if (rtup.usys_device_ID == 0x74) device_num = 1;
                else
                {
                    log = device_judge[device_num];
                    return log;
                }
                if (hex_cksum.Length >= 3)
                {
                    start_cksum = hex_cksum.Substring(0, hex_cksum.Length - 2);
                    last_cksum = hex_cksum.Substring(hex_cksum.Length - 2);
                    if (start_cksum.Length == 1)
                    {
                        start_cksum = "0" + start_cksum;
                    }
                }
                else
                {
                    start_cksum = "00";
                    last_cksum = hex_cksum;
                    if (last_cksum.Length == 1)
                    {
                        last_cksum = "0" + last_cksum;
                    }
                }
                if (start_cksum != txt.Split('-')[rtup.length - 3] || last_cksum != txt.Split('-')[rtup.length - 2])
                {
                    log = "CheckSum 오류";
                    return log;
                }
                //로그를 띄워주자 (체크섬 오류)

                if (rtup.sensor_ID == 1)
                {
                    if (rtup.response_channel == 0 || rtup.response_channel == 1)
                    {
                        if (rtup.length == 8) log = "Device '" + device + "'에서 " + rtup.response_channel + "채널에서 " + logMsg[rtup.sensor_ID + 2];
                        else log = "Device '" + device + "'에서 " + rtup.response_channel + "채널로 " + logMsg[rtup.sensor_ID - 1]; // device_judge[device_num]
                    }
                    else log = "Format 오류";
                }
                else if (rtup.sensor_ID == 2)
                {
                    if (rtup.response_channel == 0 || rtup.response_channel == 1)
                    {
                        if (rtup.length == 9 && rtup.data >= 4 && rtup.data <= 20) log = "Device '" + device + "'에서 " + rtup.response_channel + "채널에서 '" + rtup.data + "mA'의 " + logMsg[rtup.sensor_ID + 2];
                        else if (rtup.length == 8) log = "Device '" + device + "'에서 " + rtup.response_channel + "채널로 " + logMsg[rtup.sensor_ID - 1];
                        else throw new Exception("continue");
                    }
                    else log = "Format 오류";
                }
                else if (rtup.sensor_ID == 3)
                {
                    if ((rtup.response_channel == 0 || rtup.response_channel == 1 || rtup.response_channel == 2 || rtup.response_channel == 3) && rtup.data < 2)
                        log = "Device '" + device + "'에서 " + rtup.response_channel + "채널에서 시그널'" + rtup.data + "'  " + logMsg[rtup.sensor_ID + 2];
                    else log = "Format 오류";
                }
                else if (rtup.sensor_ID == 4)
                {
                    if (rtup.response_channel == 0 || rtup.response_channel == 1 || rtup.response_channel == 2 || rtup.response_channel == 3)
                        log = "Device '" + device + "'에서 " + rtup.response_channel + "채널로 시그널'" + rtup.data + "'  " + logMsg[rtup.sensor_ID - 2];
                    else log = "Format 오류";
                }
                else log = "SensorID 오류";

                return log;
            }
        }

        private ResultSet SplitAndCksum(SocketInfo socketInfo, Queue<byte> recvBuff)
        {
            int resultSet = 0;
            int dec_cksum = 0;
            string strHexSplit = string.Empty;
            string hex_cksum = string.Empty;
            try
            {
                for (int i = 0; i < recvBuff.Count; i++)
                {
                    // 먼저 sof를 버퍼에서 가져온다.
                    byte sof_data = recvBuff.Dequeue();
                    i++;
                    // 프로토콜 정의 0x02와 맞으면
                    if (sof_data == 0x02)
                    {
                        // 다음 값을 장비ID-길이일 것이라 예상하고 버퍼에서 2바이트를 가져온다.
                        CheckQueue(socketInfo, recvBuff);
                        byte device_data = recvBuff.Dequeue();
                        i++;
                        CheckQueue(socketInfo, recvBuff);
                        byte length_data = recvBuff.Dequeue();
                        i++;
                        // 길이가 8이면
                        if (length_data == 0x08)
                        {
                            // 그 다음 버퍼 값이 센서ID-채널 값이라 예상하고 버퍼에서 2바이트 더 가져온다.
                            CheckQueue(socketInfo, recvBuff);
                            byte sensor_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte ch_data = recvBuff.Dequeue();
                            i++;
                            // 지금 껏 나온 값들로 프로토콜 체크섬공식에 따라 계산한다.
                            dec_cksum = sof_data + device_data + length_data + sensor_data + ch_data;
                            //나머지 체크섬 버퍼 2바이트와 eof버퍼를 가져온다.
                            CheckQueue(socketInfo, recvBuff);
                            byte ck1_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte ck2_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte eof_data = recvBuff.Dequeue();
                            //체크섬 뒤에 eof 값이 나와야하므로 3이 아니면 지금까지 읽은 값들은 잘못된 값이다.
                            if (eof_data == 0x03)
                            {
                                byte[] receiveBuff = new byte[8] { sof_data, device_data, length_data, sensor_data, ch_data, ck1_data, ck2_data, eof_data };
                                socketInfo.r_Buff = receiveBuff;
                                strHexSplit = BitConverter.ToString(receiveBuff);
                                hex_cksum = String.Format("{0:x2}", dec_cksum).ToUpper();
                                break;
                            }
                        }
                        // 길이가 9면
                        else if (length_data == 0x09)
                        {
                            // 그 다음 버퍼 값이 센서ID-채널-데이터 값이라 예상하고 버퍼에서 3바이트 더 가져온다.
                            CheckQueue(socketInfo, recvBuff);
                            byte sensor_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte ch_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte _data = recvBuff.Dequeue();
                            i++;
                            // 지금 껏 나온 값들로 프로토콜 체크섬공식에 따라 계산한다.
                            dec_cksum = sof_data + device_data + length_data + sensor_data + ch_data + _data;
                            //나머지 체크섬 버퍼 2바이트와 eof버퍼를 가져온다.
                            CheckQueue(socketInfo, recvBuff);
                            byte ck1_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte ck2_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte eof_data = recvBuff.Dequeue();
                            i++;
                            //체크섬 뒤에 eof 값이 나와야하므로 3이 아니면 지금까지 읽은 값들은 잘못된 값이다.
                            if (eof_data == 0x03)
                            {
                                byte[] receiveBuff = new byte[9] { sof_data, device_data, length_data, sensor_data, ch_data,_data, ck1_data, ck2_data, eof_data };
                                socketInfo.r_Buff = receiveBuff;
                                strHexSplit = BitConverter.ToString(receiveBuff);
                                hex_cksum = String.Format("{0:x2}", dec_cksum).ToUpper();
                                break;
                            }
                        }
                        else if(length_data == 0x0D)
                        {
                            CheckQueue(socketInfo, recvBuff);
                            byte sensor_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte packetM_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte slave_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte func_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte startH_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte startL_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte lengthH_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte lengthL_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte crc_data1 = recvBuff.Dequeue();
                            i++; CheckQueue(socketInfo, recvBuff);
                            byte crc_data2 = recvBuff.Dequeue();
                            i++;
                            byte[] sendBuff = new byte[13] {sof_data, device_data,length_data, sensor_data, packetM_data, slave_data, func_data, startH_data,
                            startL_data, lengthH_data, lengthL_data, crc_data1, crc_data2};
                            socketInfo.r_Buff = sendBuff;
                            strHexSplit = BitConverter.ToString(sendBuff);
                            hex_cksum = "EE";
                            break;
                        }
                    
                
                        else if (length_data == 0x0E)
                        {
                            CheckQueue(socketInfo, recvBuff);
                            byte sensor_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte packet_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte slave_data = recvBuff.Dequeue();
                            i++;                           
                            CheckQueue(socketInfo, recvBuff);
                            byte function_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte bcount_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte data1_h_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte data1_l_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte data2_h_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte data2_l_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte crc1_data = recvBuff.Dequeue();
                            i++;
                            CheckQueue(socketInfo, recvBuff);
                            byte crc2_data = recvBuff.Dequeue();
                            i++;
                            //if eof 삭제
                            byte[] receiveBuff = new byte[14] { sof_data, device_data, length_data, sensor_data, packet_data, slave_data, function_data, bcount_data,
                                                                data1_h_data, data1_l_data, data2_h_data, data2_l_data, crc1_data, crc2_data};
                            socketInfo.r_Buff = receiveBuff;
                            strHexSplit = BitConverter.ToString(receiveBuff);
                            hex_cksum = String.Format("{0:x2}", 238).ToUpper();
                            break;
                        }
                    }
                }
                
                if (strHexSplit != string.Empty )
                {
                    resultSet = Convert.ToInt32(strHexSplit.Split('-')[3], 16);
                }
                else
                {
                    throw new FormatException();
                }
                if(hex_cksum == string.Empty || resultSet == 0)
                {
                    throw new FormatException();
                }
                return new ResultSet(strHexSplit, hex_cksum, resultSet);
            }
            catch(Exception e)
            {
                return new ResultSet(string.Empty, string.Empty, 0);
            }
        }

        //private string CRC16Check(string txt, SocketInfo socketInfo)
        //{
        //    ModbusRec mod = new ModbusRec();
        //    string log = string.Empty;
        //    string device = socketInfo.IP + ":" + socketInfo.PORT;
        //    mod.usys_device_ID = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[1], 16));
        //    mod.length = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[2], 16));
        //    mod.sensor_ID = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[3], 16));
        //    mod.packet = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[4], 16));




        //    mod.s_address = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[5], 16));
        //    mod.rs_function = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[6], 16));
        //    mod.byte_count = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[7], 16));
        //    mod.data1_h = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[8], 16));
        //    mod.data1_l = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[9], 16));
        //    mod.data2_h = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[10], 16));
        //    mod.data2_l = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[11], 16));
        //    mod.crc16[0] = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[12], 16));
        //    mod.crc16[1] = Convert.ToByte(Convert.ToInt32("0x" + txt.Split('-')[13], 16));

        //    byte[] crc_sum = new byte[] { mod.s_address, mod.rs_function, mod.byte_count, mod.data1_h, mod.data1_l, mod.data2_h, mod.data2_l };
        //    //string device = socketInfo.IP + ":" + socketInfo.PORT;

        //    int device_num = 0;
        //    if (mod.usys_device_ID == 0x74) device_num = 1;

        //    TModbusRTU modbusRTU = new TModbusRTU();
        //    byte[] ret = modbusRTU.MakeCRC16_byte(crc_sum, 7);
        //    if((ret[0] == mod.crc16[0]) && (ret[1] == mod.crc16[1]))
        //    {
        //        log = "CRC16통과";
        //    }
        //    else
        //    {
        //        log = "CRC Check 오류";
        //    }
        //    return log;
        //}
    }
}
