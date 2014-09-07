using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
            string addr, name, pass;
            int port;
            FileInfo serverConfigFile = new FileInfo(Environment.CurrentDirectory + "\\server.txt");
            if (serverConfigFile.Exists)
            {
                using (StreamReader reader = new StreamReader(serverConfigFile.OpenRead()))
                {
                    addr = reader.ReadLine();
                    port = int.Parse(reader.ReadLine());
                    name = reader.ReadLine();
                    pass = reader.ReadLine();
                }
            }
            else
            {
                Console.WriteLine("Enter server address:");
                addr = Console.ReadLine();
                Console.WriteLine("Enter server port (leave blank for default (64738)):");
                string line = Console.ReadLine();
                if (line == "")
                {
                    port = 64738;
                }
                else
                {
                    port = int.Parse(line);
                }
                Console.WriteLine("Enter name:");
                name = Console.ReadLine();
                Console.WriteLine("Enter password:");
                pass = Console.ReadLine();

                using (StreamWriter writer = new StreamWriter(serverConfigFile.OpenWrite()))
                {
                    writer.WriteLine(addr);
                    writer.WriteLine(port);
                    writer.WriteLine(name);
                    writer.WriteLine(pass);
                }
            }

            MumbleConnection connection = new MumbleConnection(new IPEndPoint(Dns.GetHostAddresses(addr).First(a => a.AddressFamily == AddressFamily.InterNetwork), port));
            ConsoleMumbleProtocol protocol = connection.Connect<ConsoleMumbleProtocol>(name, pass, addr);

            Thread t = new Thread(a => UpdateLoop(connection)) {IsBackground = true};
            t.Start();

            while (protocol.LocalUser == null)
            {
            }

            Console.WriteLine("Connected as " + protocol.LocalUser.Id);

            DrawChannel("", protocol.Channels.ToArray(), protocol.Users.ToArray(), protocol.RootChannel);


            const int min = 100;
            const int max = 300;
            DateTime time = DateTime.Now;
            TimeSpan duration = TimeSpan.FromSeconds(3);
            Random r = new Random();

            string[] strings = new string[]
            {
                "We Need Wards",
                "Be Adaptable",
                "Consider Smoke Ganking",
                "Do Roshan?",
                "Why Aren't You Carrying A Teleport Scroll?",
                "Group Up",
                "I mean YOLO, Right?",
                "Split Push!",
                "Use Your Wand",
            };

            while (true)
            {
                var secs = (DateTime.Now - time);
                if (secs > duration)
                {
                    duration = TimeSpan.FromSeconds(r.Next(min, max));
                    //connection.SendTextMessage(strings[r.Next(strings.Length)]);
                    time = DateTime.Now;
                }

                //Console.Title = (duration - (DateTime.Now - time)).TotalSeconds.ToString(CultureInfo.InvariantCulture);
                Console.Title = protocol.TcpPing.ToString() + "ms";

                Thread.Sleep(900);
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
            while (connection.State != ConnectionStates.Disconnected)
            {
                connection.Process();
            }
        }
    }
}
