using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using MumbleSharp;
using System.Net;
using System.Threading;

namespace MumbleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //MumbleConnection connection = new MumbleConnection(new IPEndPoint(Dns.GetHostAddresses("mumble.placeholder-software.co.uk").First(), 64738));
            MumbleConnection connection = new MumbleConnection(new IPEndPoint(Dns.GetHostAddresses("andimiller.net").First(), 25565));
            //MumbleConnection connection = new MumbleConnection(new IPEndPoint(Dns.GetHostAddresses("localhost").First(a => a.AddressFamily == AddressFamily.InterNetwork), 64738));
            connection.Connect<MumbleProtocol>("MumbleSharpClient", "", "");

            Thread t = new Thread(a => UpdateLoop(connection)) { IsBackground = true };
            t.Start();

            while (connection.Protocol.LocalUser == null) { }

            while (true)
            {
                string msg = Console.ReadLine();
                connection.SendTextMessage(msg);
            }
        }

        private static void UpdateLoop(MumbleConnection connection)
        {
            while (true)
                connection.Process();
        }
    }
}
