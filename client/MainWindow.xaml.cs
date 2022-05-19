using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace client
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        String args = "";
        Client client;
        Socket socket;
        bool _isConnected = false;
        DispatcherTimer timer = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            ipaddr.Focus();
            timer.IsEnabled = true;
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Start();
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (MessageBox.Show("정말로 창을 닫을까요?", "창 종료 확인", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
            }
            else
            {
                if(isConnected())
                {
                    client.SendMessage("exit" + "\r\n");
                    ConnectServer(false);
                }
                System.Environment.Exit(0);
            }
        }

        public void Timer_Tick(object sender, EventArgs e)
        {
            if (args != "")
            {
                addLogs(args);
                args = "";
            }
            if (isConnected())
            {
                Connect.Content = "연결해제";
                Send.IsEnabled = true;
            }
            else
            {
                Connect.Content = "연결";
                Send.IsEnabled = false;
            }
            //timer.IsEnabled = _isConnected;
        }
        public bool isConnected()
        {
            return _isConnected;
        }

        public void ConnectServer(bool cnvt)
        {
            _isConnected = cnvt;
        }
        public void addLogs(String str)
        {
            log.AppendText(str + Environment.NewLine);
            log.Select(log.Text.Length, 0);
            log.ScrollToEnd();
        }

        public void setLogs(String str)
        {
            args = str;
        }
       
        public void Connect_Server(string ipaddr)
        {
            try
            {
                client = new Client(new IPEndPoint(IPAddress.Parse(ipaddr), 10000), this);
                _isConnected = true;
            }
            catch(Exception ex)
            {
                setLogs("Wrong IP Address Type. Please check it again. \r\n");
            }
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.ConnectAsync(client.sk);
                //client.SendMessage("ipconfig");
            }
            catch (Exception ex)
            {
                _isConnected = false;
                setLogs("Failed To Connect to Server " + ipaddr + ". Check the server status again. \r\n");
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if(!isConnected())
            {
                Connect_Server(ipaddr.Text);
            }
            else
            {
                client.SendMessage("exit" + "\r\n");
                ConnectServer(false);
            }
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            Send_Message();
        }
        private void Send_Message()
        {
            client.SendMessage(content.Text + "\r\n");
            
            if ("exit".Equals(content.Text, StringComparison.OrdinalIgnoreCase))
            {
                _isConnected = false;
            }
            content.Text = "";
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                if (isConnected())
                {
                    Send_Message();
                    content.Focus();
                }
                else
                {
                    Connect_Server(ipaddr.Text);
                    if (isConnected()) content.Focus();
                    else ipaddr.Focus();
                }
            }
        }
        public class Client : MainWindow
        {
            bool firstinit = true;
            public SocketAsyncEventArgs sk = new SocketAsyncEventArgs();
            // 메시지는 개행으로 구분한다.
            private char CR = (char)0x0D;
            private char LF = (char)0x0A;
            private Socket socket;
            // 메시지를 모으기 위한 버퍼
            private StringBuilder sb = new StringBuilder();
            MainWindow mw;
            public Client(EndPoint pep, MainWindow mainWindow)
            {
                sk.RemoteEndPoint = pep;
                // 접속시 발생하는 이벤트를 등록한다.
                sk.Completed += Connected_Completed;
                //base.SetBuffer(new byte[1024], 0, 1024);
                //base.Completed += Client_Completed;
                //this.socket.ReceiveAsync(this);
                mw = mainWindow;
            }
            private void Connected_Completed(object sender, SocketAsyncEventArgs e)
            {
                // 접속 이벤트는 해제한다.
                sk.Completed -= Connected_Completed;
                // 접속 소켓 설정
                this.socket = e.ConnectSocket;
                sk.UserToken = this.socket;
                // 버퍼 설정
                sk.SetBuffer(new byte[1024], 0, 1024);
                // 수신 이벤트를 등록한다.
                sk.Completed += Client_Completed;
                // 메시지가 오면 이벤트를 발생시킨다. (IOCP로 넣는 것)
                try
                {
                    this.socket.ReceiveAsync(this.sk);
                    ConnectServer(true);
                }
                catch (NullReferenceException ex)
                {
                    ConnectServer(false);
                    mw.setLogs("Failed To Connect to Server " + sk.RemoteEndPoint.ToString() + ". Check the server status again... \r\n");
                }
                catch (Exception ex)
                {
                    ConnectServer(false);
                    mw.setLogs("Failed To Connect to Server " + sk.RemoteEndPoint.ToString() + ". Check the server status again... \r\n");
                }
            }
            private void Client_Completed(object sender, SocketAsyncEventArgs e)
            {
                // 접속이 연결되어 있으면...
                if (socket.Connected && sk.BytesTransferred > 0)
                {
                    // 수신 데이터는 e.Buffer에 있다.
                    byte[] data = e.Buffer;
                    // 데이터를 string으로 변환한다.
                    string msg = Encoding.UTF8.GetString(data);
                    // 메모리 버퍼를 초기화 한다. 크기는 1024이다
                    sk.SetBuffer(new byte[1024], 0, 1024);
                    sb.Append(msg.Trim('\0'));
                    // 메시지의 끝이 이스케이프 \r\n와 >의 형태이면 클라이언트에 표시한다.
                    if (sb.Length >= 3 && sb[sb.Length - 3] == CR && sb[sb.Length - 2] == LF && sb[sb.Length - 1] == '>')
                    {
                        msg = sb.ToString();
                        mw.setLogs(msg);
                        // 버퍼 초기화
                        sb.Clear();
                    }
                    // 메시지가 오면 이벤트를 발생시킨다. (IOCP로 넣는 것)
                    try
                    {
                        this.socket.ReceiveAsync(this.sk);
                        if(firstinit)
                        {
                            firstinit = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        ConnectServer(false);
                        mw.setLogs("Failed To Connect to Server " + sk.RemoteEndPoint.ToString() + ". Check the server status again.. \r\n");
                    }
                }
                else
                {
                    // 접속이 끊겼다..
                    var remoteAddr = (IPEndPoint)socket.RemoteEndPoint;
                    mw.setLogs($"Disconnected : (From: {remoteAddr.Address.ToString()}:{remoteAddr.Port}, Connection time: {DateTime.Now})" + Environment.NewLine + Environment.NewLine);
                }
            }
            public void SendMessage(String msg)
            {
                byte[] sendData = Encoding.UTF8.GetBytes(msg);
                //sendArgs.SetBuffer(sendData, 0, sendData.Length);
                //socket.SendAsync(sendArgs);
                try
                {
                    socket.Send(sendData, sendData.Length, SocketFlags.None);
                }
                catch(NullReferenceException ex)
                {
                    ConnectServer(false);
                    mw.setLogs("Failed To Connect to Server " + sk.RemoteEndPoint.ToString() + ". Check the server status again. \r\n");
                }
                catch(Exception ex)
                {
                    ConnectServer(false);
                    mw.setLogs("Failed To Connect to Server " + sk.RemoteEndPoint.ToString() + ". Check the server status again. \r\n");
                }
            }
        }
    }
}
