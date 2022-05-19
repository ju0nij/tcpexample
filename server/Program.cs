using System;
using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Security.Principal;

namespace server
{
    internal class Program : Socket
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        public Program() : base(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            // 포트는 10000을 Listen한다.
            base.Bind(new IPEndPoint(IPAddress.Any, 10000));
            base.Listen(20);
            base.AcceptAsync(new Server(this));
        }
        [STAThread]
        static void Main()
        {
            new Program();
            // q키를 누르면 서버는 종료한다.
            Console.WriteLine("Press the q key to exit.");
            while (true)
            {
                Console.WriteLine("TCP example, press q to exit");
                string k = Console.ReadLine();
                if ("q".Equals(k, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                
            }
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
            remoteAddr = (IPEndPoint)socket.RemoteEndPoint;
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
        Client client;
        public Server(Socket socket)
        {
            this.socket = socket;
            base.UserToken = socket;
            base.Completed += Server_Completed; ;
        }
        private void Server_Completed(object sender, SocketAsyncEventArgs e)
        {
            client = new Client(e.AcceptSocket);
            e.AcceptSocket = null;
            this.socket.AcceptAsync(e);
        }
    }
}

