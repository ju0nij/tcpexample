using System;
using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Security.Principal;

namespace tcpexample
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Proc.start();
        }
    }
    public class Client : SocketAsyncEventArgs
    {
        private static char CR = (char)0x0D;
        private static char LF = (char)0x0A;
        private Socket socket;
        private StringBuilder sb = new StringBuilder();
        private IPEndPoint remoteAddr;
        public Client(Socket socket)
        {
            this.socket = socket;
            base.SetBuffer(new byte[1024], 0, 1024);
            base.UserToken = socket;
            base.Completed += Client_Completed; ;
            this.socket.ReceiveAsync(this);
            // 접속 환영 메시지
            remoteAddr = (IPEndPoint)socket.RemoteEndPoint;
            //MainWindow.log.Warn($"Client : (From: {remoteAddr.Address.ToString()}:{remoteAddr.Port}, Connection time: {DateTime.Now})");
            this.Send("Welcome server!\r\n>");
        }
        private void Client_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (socket.Connected && base.BytesTransferred > 0)
            {
                byte[] data = e.Buffer;
                string msg = Encoding.UTF8.GetString(data);
                base.SetBuffer(new byte[1024], 0, 1024);
                sb.Append(msg.Trim('\0'));
                if (sb.Length >= 2 && sb[sb.Length - 2] == CR && sb[sb.Length - 1] == LF)
                {
                    sb.Length = sb.Length - 2;
                    msg = sb.ToString();
                    // 만약 메시지가 exit이면 접속을 끊는다.
                    if ("exit".Equals(msg, StringComparison.OrdinalIgnoreCase))
                    {
                        socket.DisconnectAsync(this);
                        return;
                    }
                    else
                    {
                        Console.WriteLine(msg);
                    }
                    sb.Clear();
                }
                this.socket.ReceiveAsync(this);
            }
            else
            {
                // 접속이 끊겼을 때
                //MainWindow.log.Debug($"Disconnected : (From: {remoteAddr.Address.ToString()}:{remoteAddr.Port}, Connection time: {DateTime.Now})");
            }
        }
        private void Send(String msg)
        {
            byte[] sendData = Encoding.UTF8.GetBytes(msg);
            //sendArgs.SetBuffer(sendData, 0, sendData.Length);
            //socket.SendAsync(sendArgs);
            // Client로 메시지 전송
            socket.Send(sendData, sendData.Length, SocketFlags.None);
        }
    }

    class Server : SocketAsyncEventArgs
    {
        private Socket socket;
        public Server(Socket socket)
        {
            this.socket = socket;
            base.UserToken = socket;
            // Client로부터 Accept이 되면 이벤트를 발생시킨다. (IOCP로 꺼내는 것)
            base.Completed += Server_Completed; ;
        }
        // Client가 접속하면 이벤트를 발생한다.
        private void Server_Completed(object sender, SocketAsyncEventArgs e)
        {
            // 접속이 완료되면, Client Event를 생성하여 Receive이벤트를 생성한다.
            var client = new Client(e.AcceptSocket);
            // 서버 Event에 cilent를 제거한다.
            e.AcceptSocket = null;
            // Client로부터 Accept이 되면 이벤트를 발생시킨다. (IOCP로 넣는 것)
            this.socket.AcceptAsync(e);
        }
    }

    class Proc : Socket
    {
        public Proc() : base(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            // 포트는 10000을 Listen한다.
            base.Bind(new IPEndPoint(IPAddress.Any, 10000));
            base.Listen(20);
            // 비동기 소켓으로 Server 클래스를 선언한다. (IOCP로 집어넣는것)
            base.AcceptAsync(new Server(this));
        }
        static public void start()
        {
            new Proc();
        }
    }
}

