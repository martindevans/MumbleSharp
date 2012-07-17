using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MumbleSharp;
using MumbleSharp.Model;

namespace MumbleClient
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Enter server address:");
            var addr = Console.ReadLine();
            Console.WriteLine("Enter server port:");
            var port = int.Parse(Console.ReadLine());
            Console.WriteLine("Enter name:");
            var name = Console.ReadLine();
            Console.WriteLine("Enter password:");
            var pass = Console.ReadLine();

            MumbleConnection connection = new MumbleConnection(new IPEndPoint(Dns.GetHostAddresses(addr).First(a => a.AddressFamily == AddressFamily.InterNetwork), port));
            connection.Connect<MumbleProtocol>(name, pass, addr);

            Thread t = new Thread(a => UpdateLoop(connection)) {IsBackground = true};
            t.Start();

            while (connection.Protocol.LocalUser == null)
            {
            }

            Console.WriteLine("Connected as " + connection.Protocol.LocalUser.Id);

            DrawChannel("", connection.Protocol.Channels.ToArray(), connection.Protocol.Users.ToArray(), connection.Protocol.RootChannel);

            while (true)
            {
                string msg = Console.ReadLine();
                connection.SendTextMessage(msg);
            }
        }

        private static void DrawChannel(string indent, IEnumerable<Channel> channels, IEnumerable<User> users, Channel c)
        {
            Console.WriteLine(indent + c.Name + (c.Temporary ? "(temp)" : ""));

            foreach (var user in users.Where(u => u.Channel == c))
                Console.WriteLine(indent + "->" + user.Name);

            foreach (var channel in channels.Where(ch => ch.Parent == c.Id && ch.Parent != ch.Id))
                DrawChannel(indent + "\t", channels, users, channel);
        }

        private static void UpdateLoop(MumbleConnection connection)
        {
            while (true)
            {
                connection.Process();
            }
        }
    }
}
