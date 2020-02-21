using System;
using System.Collections.Generic;
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
        private static Exception _updateLoopThreadException = null;

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

            ConsoleMumbleProtocol protocol = new ConsoleMumbleProtocol();
            MumbleConnection connection = new MumbleConnection(new IPEndPoint(Dns.GetHostAddresses(addr).First(a => a.AddressFamily == AddressFamily.InterNetwork), port), protocol);
            connection.Connect(name, pass, new string[0], addr);

            //Start the UpdateLoop thread, and collect a possible exception at termination
            ThreadStart updateLoopThreadStart = new ThreadStart(() => UpdateLoop(connection, out _updateLoopThreadException));
            updateLoopThreadStart += () => {
                if(_updateLoopThreadException != null)
                    throw new Exception($"{nameof(UpdateLoop)} was terminated unexpectedly because of a {_updateLoopThreadException.GetType().ToString()}", _updateLoopThreadException);
            };
            Thread updateLoopThread = new Thread(updateLoopThreadStart) {IsBackground = true};
            updateLoopThread.Start();

            var r = new MicrophoneRecorder(protocol);

            //When localuser is set it means we're really connected
            while (!protocol.ReceivedServerSync)
            {
            }

            Console.WriteLine("Connected as " + protocol.LocalUser.Id);

            DrawChannel("", protocol.Channels.ToArray(), protocol.Users.ToArray(), protocol.RootChannel);

            Console.ReadLine();
        }

        private static void DrawChannel(string indent, IEnumerable<Channel> channels, IEnumerable<User> users, Channel c)
        {
            Console.WriteLine(indent + c.Name + (c.Temporary ? "(temp)" : ""));

            foreach (var user in users.Where(u => u.Channel.Equals(c)))
            {
                if (string.IsNullOrWhiteSpace(user.Comment))
                    Console.WriteLine(indent + "-> " + user.Name);
                else
                    Console.WriteLine(indent + "-> " + user.Name + " (" + user.Comment + ")");
            }

            foreach (var channel in channels.Where(ch => ch.Parent == c.Id && ch.Parent != ch.Id))
                DrawChannel(indent + "\t", channels, users, channel);
        }

        private static void UpdateLoop(MumbleConnection connection, out Exception exception)
        {
            exception = null;
            try
            {
                while (connection.State != ConnectionStates.Disconnected)
                {
                    if (connection.Process())
                        Thread.Yield();
                    else
                        Thread.Sleep(1);
                }
            } catch (Exception ex)
            {
                exception = ex;
            }
        }
    }
}
