using System.Net;
using System.Net.Sockets;
using MumbleSharp.Packets;
using System;
using ProtoBuf;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MumbleSharp.Model;

namespace MumbleSharp
{
    public class MumbleConnection
    {
        public ConnectionState State;

        TcpSocket tcp;
        UdpSocket udp;

        DateTime lastSentPing = DateTime.MinValue;

        public IMumbleProtocol Protocol { get; private set; }

        public IPEndPoint Host
        {
            get;
            private set;
        }

        public MumbleConnection(IPEndPoint host)
        {
            Host = host;
            State = ConnectionState.Connecting;
        }

        public void Connect<P>(string username, string password, string serverName) where P : IMumbleProtocol, new()
        {
            Protocol = new P();
            Protocol.Initialise(this);

            tcp = new TcpSocket(Host, Protocol);
            tcp.Connect(username, password, serverName);

            udp = new UdpSocket(Host, Protocol);
            udp.Connect();

            State = ConnectionState.Connected;
        }

        public void Close()
        {
            State = ConnectionState.Disconnected;

            udp.Close();
            tcp.Close();
        }

        public void Process()
        {
            if ((DateTime.Now - lastSentPing).TotalSeconds > 20)
            {
                tcp.SendPing();
                udp.SendPing();
                lastSentPing = DateTime.Now;
            }
            
            tcp.Process();
            udp.Process();
        }

        public void SendTextMessage(string message, Channel channel = null)
        {
            tcp.Send<TextMessage>(PacketType.TextMessage, new TextMessage() { Session = new[] { Protocol.LocalUser.Id }, Message = new[] { message }, ChannelId = new uint[] { channel == null ? 0 : channel.Id } });
        }

        internal void SendControl<T>(PacketType type, T packet)
        {
            tcp.Send<T>(type, packet);
        }

        private class TcpSocket
        {
            TcpClient client;
            IPEndPoint host;

            NetworkStream netStream;
            SslStream ssl;
            BinaryReader reader;
            BinaryWriter writer;

            IMumbleProtocol protocol;

            public TcpSocket(IPEndPoint host, IMumbleProtocol protocol)
            {
                this.host = host;
                this.protocol = protocol;
                client = new TcpClient();
            }

            private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
            {
                return true;
            }

            public void Connect(string username, string password, string serverName)
            {
                client.Connect(host);

                netStream = client.GetStream();
                ssl = new SslStream(netStream, false, ValidateCertificate);
                ssl.AuthenticateAsClient(serverName);
                reader = new BinaryReader(ssl);
                writer = new BinaryWriter(ssl);

                Handshake(username, password);
            }

            public void Close()
            {
                reader.Close();
                writer.Close();
                ssl = null;
                netStream.Close();
                client.Close();
            }

            private void Handshake(string username, string password)
            {
                MumbleSharp.Packets.Version version = new MumbleSharp.Packets.Version()
                {
                    Release = "MumbleSharp",
                    ReleaseVersion = (0 << 16) | (0 << 8) | (1 & 0xFF),
                    os = "Windows",
                    os_version = "7",
                };
                Send<MumbleSharp.Packets.Version>(PacketType.Version, version);

                Authenticate auth = new Authenticate()
                {
                    Username = username,
                    Password = password,
                    Tokens = new string[0],
                    CeltVersions = new int[] { unchecked((int)0x8000000b) },
                };
                Send<Authenticate>(PacketType.Authenticate, auth);
            }

            public void Send<T>(PacketType type, T packet)
            {
                lock (ssl)
                {
                    writer.Write(IPAddress.HostToNetworkOrder((short)type));
                    writer.Flush();

                    Serializer.SerializeWithLengthPrefix<T>(ssl, packet, PrefixStyle.Fixed32BigEndian);
                    ssl.Flush();
                    netStream.Flush();
                }
            }

            public void SendPing()
            {
                lock (ssl)
                    Send<Ping>(PacketType.Ping, new Ping());
            }

            public void Process()
            {
                if (!client.Connected)
                    throw new InvalidOperationException("Not connected");

                if (!netStream.DataAvailable)
                    return;

                lock (ssl)
                {
                    PacketType type = PacketType.Empty;
                    try
                    {
                         type = (PacketType)IPAddress.NetworkToHostOrder(reader.ReadInt16());
                         Console.WriteLine(type.ToString());
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine("shit");
                    }
                    switch (type)
                    {
                        case PacketType.Version:
                            protocol.Version(Serializer.DeserializeWithLengthPrefix<MumbleSharp.Packets.Version>(ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.CryptSetup:
                            protocol.CryptSetup(Serializer.DeserializeWithLengthPrefix<MumbleSharp.Packets.CryptSetup>(ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.ChannelState:
                            protocol.ChannelState(Serializer.DeserializeWithLengthPrefix<MumbleSharp.Packets.ChannelState>(ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.UserState:
                            protocol.UserState(Serializer.DeserializeWithLengthPrefix<MumbleSharp.Packets.UserState>(ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.CodecVersion:
                            protocol.CodecVersion(Serializer.DeserializeWithLengthPrefix<MumbleSharp.Packets.CodecVersion>(ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.ContextAction:
                            protocol.ContextAction(Serializer.DeserializeWithLengthPrefix<MumbleSharp.Packets.ContextAction>(ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.ContextActionAdd:
                            protocol.ContextActionAdd(Serializer.DeserializeWithLengthPrefix<MumbleSharp.Packets.ContextActionAdd>(ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.PermissionQuery:
                            protocol.PermissionQuery(Serializer.DeserializeWithLengthPrefix<MumbleSharp.Packets.PermissionQuery>(ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.ServerSync:
                            protocol.ServerSync(Serializer.DeserializeWithLengthPrefix<MumbleSharp.Packets.ServerSync>(ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.ServerConfig:
                            protocol.ServerConfig(Serializer.DeserializeWithLengthPrefix<MumbleSharp.Packets.ServerConfig>(ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.UDPTunnel:
                            var length = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                            protocol.UdpTunnel(new UdpTunnel() { Packet = reader.ReadBytes(length) });
                            break;
                        case PacketType.Ping:
                            protocol.Ping(Serializer.DeserializeWithLengthPrefix<MumbleSharp.Packets.Ping>(ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.UserRemove:
                            protocol.UserRemove(Serializer.DeserializeWithLengthPrefix<MumbleSharp.Packets.UserRemove>(ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.ChannelRemove:
                            protocol.ChannelRemove(Serializer.DeserializeWithLengthPrefix<MumbleSharp.Packets.ChannelRemove>(ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.TextMessage:
                            protocol.TextMessage(Serializer.DeserializeWithLengthPrefix<MumbleSharp.Packets.TextMessage>(ssl, PrefixStyle.Fixed32BigEndian));
                            break;

                        case PacketType.Reject:
                            throw new NotImplementedException();

                        case PacketType.Authenticate:
                        case PacketType.PermissionDenied:
                        case PacketType.ACL:
                        case PacketType.QueryUsers:
                        case PacketType.UserList:
                        case PacketType.VoiceTarget:
                        case PacketType.UserStats:
                        case PacketType.RequestBlob:
                        case PacketType.BanList:
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }

        private class UdpSocket
        {
            UdpClient client;
            IPEndPoint host;
            IMumbleProtocol protocol;

            public UdpSocket(IPEndPoint host, IMumbleProtocol protocol)
            {
                this.host = host;
                this.protocol = protocol;
                client = new UdpClient();
            }

            public void Connect()
            {
                client.Connect(host);
            }

            public void Close()
            {
                client.Close();
            }

            public void SendPing()
            {
                long timestamp = DateTime.Now.Ticks;

                byte[] buffer = new byte[9];
                buffer[0] = 1 << 5;
                buffer[1] = (byte)((timestamp >> 56) & 0xFF);
                buffer[2] = (byte)((timestamp >> 48) & 0xFF);
                buffer[3] = (byte)((timestamp >> 40) & 0xFF);
                buffer[4] = (byte)((timestamp >> 32) & 0xFF);
                buffer[5] = (byte)((timestamp >> 24) & 0xFF);
                buffer[6] = (byte)((timestamp >> 16) & 0xFF);
                buffer[7] = (byte)((timestamp >> 8) & 0xFF);
                buffer[8] = (byte)((timestamp) & 0xFF);

                client.Send(buffer, buffer.Length);
            }

            public void Process()
            {
                if (client.Client == null)
                    return;
                if (client.Available == 0)
                    return;

                IPEndPoint sender = host;
                byte[] data = client.Receive(ref sender);

                protocol.Udp(data);
            }
        }
    }
}
