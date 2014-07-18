using System;
using System.Collections.Generic;
using System.Globalization;
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
            var addr = "mumble.placeholder-software.co.uk";//Console.ReadLine();
            Console.WriteLine("Enter server port:");
            var port = 64738;//int.Parse(Console.ReadLine());
            Console.WriteLine("Enter name:");
            var name = ".AI";//Console.ReadLine();
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


            const int min = 100;
            const int max = 300;
            DateTime time = DateTime.Now;
            TimeSpan duration = TimeSpan.FromSeconds(3);
            Random r = new Random();

            string[] strings = new string[]
            {
                //"We Need Wards",
                //"Be Adaptable",
                //"Consider Smoke Ganking",
                //"Do Roshan?",
                //"Why Aren't You Carrying A Teleport Scroll?",
                "Mordred, Stop Farming The Jungle",
                //"Group Up",
                "Matt, Why Are You So Overextended?!",
                //"I mean YOLO, Right?",
                //"Split Push!",
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
                Console.Title = ((MumbleProtocol) connection.Protocol).TcpPing.ToString() + "ms";

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
