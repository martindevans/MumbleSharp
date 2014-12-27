using MumbleSharp.Audio.Codecs;
using MumbleSharp.Packets;
using ProtoBuf;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace MumbleSharp
{
    /// <summary>
    /// Handles the low level details of connecting to a mumble server. Once connection is established decoded packets are passed off to the MumbleProtocol for processing
    /// </summary>
    public class MumbleConnection
    {
        public ConnectionStates State { get; private set; }

        TcpSocket _tcp;
        UdpSocket _udp;

        DateTime _lastSentPing = DateTime.MinValue;

        public IMumbleProtocol Protocol { get; private set; }

        public IPEndPoint Host
        {
            get;
            private set;
        }

        readonly CryptState _cryptState = new CryptState();

        /// <summary>
        /// Creates a connection to the server using the given address and port.
        /// </summary>
        /// <param name="server">The server adress or IP.</param>
        /// <param name="port">The port the server listens to.</param>
        /// <param name="protocol">An object which will handle messages from the server</param>
        public MumbleConnection(string server, int port, IMumbleProtocol protocol)
            : this(new IPEndPoint(Dns.GetHostAddresses(server).First(a => a.AddressFamily == AddressFamily.InterNetwork), port), protocol)
        {
        }

        /// <summary>
        /// Creates a connection to the server
        /// </summary>
        /// <param name="host"></param>
        /// <param name="protocol"></param>
        public MumbleConnection(IPEndPoint host, IMumbleProtocol protocol)
        {
            Host = host;
            State = ConnectionStates.Disconnected;
            Protocol = protocol;
        }

        public void Connect(string username, string password, string[] tokens, string serverName)
        {
            if (State != ConnectionStates.Disconnected)
                throw new InvalidOperationException(string.Format("Cannot start connecting MumbleConnection when connection state is {0}", State));

            State = ConnectionStates.Connecting;
            Protocol.Initialise(this);

            _tcp = new TcpSocket(Host, Protocol, this);
            _tcp.Connect(username, password, tokens, serverName);

            // UDP Connection is disabled while decryption is broken
            // See: https://github.com/martindevans/MumbleSharp/issues/4
            // UDP being disabled does not reduce functionality, it forces packets to be sent over TCP instead
            _udp = new UdpSocket(Host, Protocol, this);
            //_udp.Connect();

            State = ConnectionStates.Connected;
        }

        public void Close()
        {
            State = ConnectionStates.Disconnecting;

            _udp.Close();
            _tcp.Close();

            State = ConnectionStates.Disconnected;
        }

        public void Process()
        {
            if ((DateTime.Now - _lastSentPing).TotalSeconds > 15)
            {
                _tcp.SendPing();

                if (_udp.IsConnected)
                    _udp.SendPing();

                _lastSentPing = DateTime.Now;
            }
            _tcp.Process();
            _udp.Process();
        }

        public void SendControl<T>(PacketType type, T packet)
        {
            _tcp.Send<T>(type, packet);
        }

        private void ReceivedEncryptedUdp(byte[] packet)
        {
            byte[] plaintext = _cryptState.Decrypt(packet, packet.Length);

            if (plaintext == null)
            {
                Console.WriteLine("Decryption failed");
                return;
            }

            ReceiveDecryptedUdp(plaintext);
        }

        private void ReceiveDecryptedUdp(byte[] packet)
        {
            int type = packet[0] >> 5 & 0x7;

            if (type == 1)
                Protocol.UdpPing(packet);
            else
            {
                var vType = (SpeechCodecs)(packet[0] >> 5 & 0x7);
                int voiceTarget = packet[0] & 0x1F;

                using (var reader = new UdpPacketReader(new MemoryStream(packet, 1, packet.Length - 1)))
                {
                    UInt32 session = (uint)reader.ReadVarInt64();
                    Int64 sequence = reader.ReadVarInt64();

                    //Null codec means the user was not found. This can happen if a user leaves while voice packets are still in flight
                    IVoiceCodec codec = Protocol.GetCodec(session, vType);
                    if (codec == null)
                        return;

                    if (vType == SpeechCodecs.Opus)
                    {
                        int size = (int)reader.ReadVarInt64();
                        size &= 0x1fff;

                        if (size == 0)
                            return;

                        byte[] data = reader.ReadBytes(size);
                        if (data == null)
                            return;

                        Protocol.EncodedVoice(data, session, sequence, codec);
                    }
                    else
                    {
                        throw new NotImplementedException("Codec is not opus");

                        //byte header;
                        //do
                        //{
                        //    header = reader.ReadByte();
                        //    int length = header & 0x7F;
                        //    if (length > 0)
                        //    {
                        //        byte[] data = reader.ReadBytes(length);
                        //        if (data == null)
                        //            break;

                        //        //TODO: Put *encoded* packets into a queue, then decode the head of the queue
                        //        //TODO: This allows packets to come into late and be inserted into the correct place in the queue (if they arrive before decoding handles a later packet)
                        //        byte[] decodedPcmData = codec.Decode(data);
                        //        if (decodedPcmData != null)
                        //            Protocol.Voice(decodedPcmData, session, sequence);
                        //    }

                        //} while ((header & 0x80) > 0);
                    }
                }
            }
        }

        internal void ProcessCryptState(CryptSetup cryptSetup)
        {
            if (cryptSetup.Key != null && cryptSetup.ClientNonce != null && cryptSetup.ServerNonce != null) // Full key setup
            {
                _cryptState.SetKeys(cryptSetup.Key, cryptSetup.ClientNonce, cryptSetup.ServerNonce);
            }
            else if (cryptSetup.ServerNonce != null) // Server syncing its nonce to us.
            {
                _cryptState.ServerNonce = cryptSetup.ServerNonce;
            }
            else // Server wants our nonce.
            {
                SendControl<CryptSetup>(PacketType.CryptSetup, new CryptSetup { ClientNonce = _cryptState.ClientNonce });
            }
        }

        private class TcpSocket
        {
            readonly TcpClient _client;
            readonly IPEndPoint _host;

            NetworkStream _netStream;
            SslStream _ssl;
            BinaryReader _reader;
            BinaryWriter _writer;

            readonly IMumbleProtocol _protocol;
            readonly MumbleConnection _connection;

            public TcpSocket(IPEndPoint host, IMumbleProtocol protocol, MumbleConnection connection)
            {
                _host = host;
                _protocol = protocol;
                _connection = connection;
                _client = new TcpClient();
            }

            private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
            {
                return _protocol.ValidateCertificate(sender, certificate, chain, errors);
            }

            private X509Certificate SelectCertificate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
            {
                return _protocol.SelectCertificate(sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers);
            }

            public void Connect(string username, string password, string[] tokens, string serverName)
            {
                _client.Connect(_host);

                _netStream = _client.GetStream();
                _ssl = new SslStream(_netStream, false, ValidateCertificate, SelectCertificate);
                _ssl.AuthenticateAsClient(serverName);
                _reader = new BinaryReader(_ssl);
                _writer = new BinaryWriter(_ssl);

                DateTime startWait = DateTime.Now;
                while (!_ssl.IsAuthenticated)
                {
                    if (DateTime.Now - startWait > TimeSpan.FromSeconds(2))
                        throw new TimeoutException("Timed out waiting for ssl authentication");
                }

                Handshake(username, password, tokens);
            }

            public void Close()
            {
                _reader.Close();
                _writer.Close();
                _ssl = null;
                _netStream.Close();
                _client.Close();
            }

            private void Handshake(string username, string password, string[] tokens)
            {
                Packets.Version version = new Packets.Version
                {
                    Release = "MumbleSharp",
                    ReleaseVersion = (1 << 16) | (2 << 8) | (0 & 0xFF),
                    os = Environment.OSVersion.ToString(),
                    os_version = Environment.OSVersion.VersionString,
                };
                Send<Packets.Version>(PacketType.Version, version);

                Authenticate auth = new Authenticate
                {
                    Username = username,
                    Password = password,
                    Tokens = tokens ?? new string[0],
                    CeltVersions = new int[] { unchecked((int)0x8000000b) },
                    Opus = true,
                };
                Send<Authenticate>(PacketType.Authenticate, auth);
            }

            public void Send<T>(PacketType type, T packet)
            {
                lock (_ssl)
                {
                    _writer.Write(IPAddress.HostToNetworkOrder((short)type));
                    _writer.Flush();

                    Serializer.SerializeWithLengthPrefix<T>(_ssl, packet, PrefixStyle.Fixed32BigEndian);
                    _ssl.Flush();
                    _netStream.Flush();
                }
            }

            public void SendPing()
            {
                lock (_ssl)
                    Send<Ping>(PacketType.Ping, new Ping());
            }

            public void Process()
            {
                if (!_client.Connected)
                    throw new InvalidOperationException("Not connected");

                if (!_netStream.DataAvailable)
                    return;

                lock (_ssl)
                {
                    PacketType type = (PacketType)IPAddress.NetworkToHostOrder(_reader.ReadInt16());
                    switch (type)
                    {
                        case PacketType.Version:
                            _protocol.Version(Serializer.DeserializeWithLengthPrefix<Packets.Version>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.CryptSetup:
                            var cryptSetup = Serializer.DeserializeWithLengthPrefix<CryptSetup>(_ssl, PrefixStyle.Fixed32BigEndian);
                            _connection.ProcessCryptState(cryptSetup);
                            SendPing();
                            break;
                        case PacketType.ChannelState:
                            _protocol.ChannelState(Serializer.DeserializeWithLengthPrefix<ChannelState>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.UserState:
                            _protocol.UserState(Serializer.DeserializeWithLengthPrefix<UserState>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.CodecVersion:
                            _protocol.CodecVersion(Serializer.DeserializeWithLengthPrefix<CodecVersion>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.ContextAction:
                            _protocol.ContextAction(Serializer.DeserializeWithLengthPrefix<ContextAction>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.ContextActionAdd:
                            _protocol.ContextActionAdd(Serializer.DeserializeWithLengthPrefix<ContextActionAdd>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.PermissionQuery:
                            _protocol.PermissionQuery(Serializer.DeserializeWithLengthPrefix<PermissionQuery>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.ServerSync:
                            _protocol.ServerSync(Serializer.DeserializeWithLengthPrefix<ServerSync>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.ServerConfig:
                            _protocol.ServerConfig(Serializer.DeserializeWithLengthPrefix<ServerConfig>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.UDPTunnel:
                            var length = IPAddress.NetworkToHostOrder(_reader.ReadInt32());
                            _connection.ReceiveDecryptedUdp(_reader.ReadBytes(length));
                            break;
                        case PacketType.Ping:
                            _protocol.Ping(Serializer.DeserializeWithLengthPrefix<Ping>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.UserRemove:
                            _protocol.UserRemove(Serializer.DeserializeWithLengthPrefix<UserRemove>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.ChannelRemove:
                            _protocol.ChannelRemove(Serializer.DeserializeWithLengthPrefix<ChannelRemove>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.TextMessage:
                            _protocol.TextMessage(Serializer.DeserializeWithLengthPrefix<TextMessage>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;

                        case PacketType.Reject:
                            throw new NotImplementedException();

                        case PacketType.UserList:
                            _protocol.UserList(Serializer.DeserializeWithLengthPrefix<UserList>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;

                        case PacketType.SuggestConfig:
                            _protocol.SuggestConfig(Serializer.DeserializeWithLengthPrefix<SuggestConfig>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;

                        case PacketType.Authenticate:
                        case PacketType.PermissionDenied:
                        case PacketType.ACL:
                        case PacketType.QueryUsers:
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
            readonly UdpClient _client;
            readonly IPEndPoint _host;
            readonly IMumbleProtocol _protocol;
            readonly MumbleConnection _connection;

            public bool IsConnected { get; private set; }

            public UdpSocket(IPEndPoint host, IMumbleProtocol protocol, MumbleConnection connection)
            {
                _host = host;
                _protocol = protocol;
                _connection = connection;
                _client = new UdpClient();
            }

            public void Connect()
            {
                _client.Connect(_host);
                IsConnected = true;
            }

            public void Close()
            {
                IsConnected = false;
                _client.Close();
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

                _client.Send(buffer, buffer.Length);
            }

            public void Process()
            {
                if (_client.Client == null)
                    return;
                if (_client.Available == 0)
                    return;

                IPEndPoint sender = _host;
                byte[] data = _client.Receive(ref sender);

                _connection.ReceivedEncryptedUdp(data);
            }
        }
    }
}
