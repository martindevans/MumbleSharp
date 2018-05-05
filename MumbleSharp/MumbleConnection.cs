using MumbleProto;
using MumbleSharp.Audio;
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
        public float? TcpPingAverage { get; set; }
        public float? TcpPingVariance { get; set; }
        public uint? TcpPingPackets { get; set; }

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
            if ((DateTime.Now - _lastSentPing).TotalSeconds > 5)
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

        public void SendVoice(ArraySegment<byte> packet)
        {
            //This is *totally wrong*
            //the packet contains raw encoded voice data, but we need to put it into the proper packet format
            //UPD: packet prepare before this method called. See basic protocol

            _tcp.SendVoice(PacketType.UDPTunnel, packet);
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
            var type = packet[0] >> 5 & 0x7;

            if (type == 1)
                Protocol.UdpPing(packet);
            else
                UnpackVoicePacket(packet, type);
        }

        private void PackVoicePacket(ArraySegment<byte> packet)
        {
        }

        private void UnpackVoicePacket(byte[] packet, int type)
        {
            var vType = (SpeechCodecs)type;
            var target = (SpeechTarget)(packet[0] & 0x1F);

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

                    Protocol.EncodedVoice(data, session, sequence, codec, target);
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

        internal void ProcessCryptState(CryptSetup cryptSetup)
        {
            if (cryptSetup.ShouldSerializeKey() && cryptSetup.ShouldSerializeClientNonce() && cryptSetup.ShouldSerializeServerNonce()) // Full key setup
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

        #region pings
        //using the approch described here to do running calculations of ping values.
        // http://dsp.stackexchange.com/questions/811/determining-the-mean-and-standard-deviation-in-real-time
        private float _meanOfPings;
        private float _varianceTimesCountOfPings;
        private int _countOfPings;
        private bool _shouldSetTimestampWhenPinging;

        private void ReceivePing(Ping ping)
        {
            _shouldSetTimestampWhenPinging = true;
            if (ping.ShouldSerializeTimestamp() && ping.Timestamp != 0)
            {
                var mostRecentPingtime =
                    (float)TimeSpan.FromTicks(DateTime.Now.Ticks - (long)ping.Timestamp).TotalMilliseconds;

                //The ping time is the one-way transit time.
                mostRecentPingtime /= 2;

                var previousMean = _meanOfPings;
                _countOfPings++;
                _meanOfPings = _meanOfPings + ((mostRecentPingtime - _meanOfPings) / _countOfPings);
                _varianceTimesCountOfPings = _varianceTimesCountOfPings +
                                             ((mostRecentPingtime - _meanOfPings) * (mostRecentPingtime - previousMean));

                TcpPingPackets = (uint)_countOfPings;
                TcpPingAverage = _meanOfPings;
                TcpPingVariance = _varianceTimesCountOfPings / _countOfPings;
            }


        }
        #endregion

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

                    System.Threading.Thread.Sleep(10);
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
                MumbleProto.Version version = new MumbleProto.Version
                {
                    Release = "MumbleSharp",
                    version = (1 << 16) | (2 << 8) | (0 & 0xFF),
                    Os = Environment.OSVersion.ToString(),
                    OsVersion = Environment.OSVersion.VersionString,
                };
                Send(PacketType.Version, version);

                Authenticate auth = new Authenticate
                {
                    Username = username,
                    Password = password,
                    Opus = true,
                };
                auth.Tokens.AddRange(tokens ?? new string[0]);
                auth.CeltVersions = new int[] { unchecked((int)0x8000000b) };

                Send(PacketType.Authenticate, auth);
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

            public void Send(PacketType type, ArraySegment<byte> packet)
            {
                lock (_ssl)
                {
                    _writer.Write(IPAddress.HostToNetworkOrder((short)type));
                    _writer.Write(IPAddress.HostToNetworkOrder(packet.Count));
                    _writer.Write(packet.Array, packet.Offset, packet.Count);

                    _writer.Flush();
                    _ssl.Flush();
                    _netStream.Flush();
                }
            }

            public void SendVoice(PacketType type, ArraySegment<byte> packet)
            {
                lock (_ssl)
                {
                    _writer.Write(IPAddress.HostToNetworkOrder((short)type));
                    _writer.Write(IPAddress.HostToNetworkOrder(packet.Count));
                    _writer.Write(packet.Array, packet.Offset, packet.Count);

                    _writer.Flush();
                    _ssl.Flush();
                    _netStream.Flush();
                }
            }

            public void SendBuffer(PacketType type, byte[] packet)
            {
                lock (_ssl)
                {
                    _writer.Write(IPAddress.HostToNetworkOrder((short)type));
                    _writer.Write(IPAddress.HostToNetworkOrder(packet.Length));
                    _writer.Write(packet, 0, packet.Length);

                    _writer.Flush();
                    _ssl.Flush();
                    _netStream.Flush();
                }
            }

            public void SendPing()
            {
                var ping = new Ping();

                //Only set the timestamp if we're currently connected.  This prevents the ping stats from being built.
                //  otherwise the stats will be throw off by the time it takes to connect.
                if (_connection._shouldSetTimestampWhenPinging)
                {
                    ping.Timestamp = (ulong) DateTime.Now.Ticks;
                }

                if (_connection.TcpPingAverage.HasValue)
                {
                    ping.TcpPingAvg = _connection.TcpPingAverage.Value;
                }
                if (_connection.TcpPingVariance.HasValue)
                {
                    ping.TcpPingVar = _connection.TcpPingVariance.Value;
                }
                if (_connection.TcpPingPackets.HasValue)
                {
                    ping.TcpPackets = _connection.TcpPingPackets.Value;
                }

                lock (_ssl)
                    Send<Ping>(PacketType.Ping, ping);
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
                    Console.WriteLine("{0:HH:mm:ss}: {1}", DateTime.Now, type.ToString());

                    switch (type)
                    {
                        case PacketType.Version:
                            _protocol.Version(Serializer.DeserializeWithLengthPrefix<MumbleProto.Version>(_ssl, PrefixStyle.Fixed32BigEndian));
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
                        case PacketType.ContextActionModify:
                            _protocol.ContextActionModify(Serializer.DeserializeWithLengthPrefix<ContextActionModify>(_ssl, PrefixStyle.Fixed32BigEndian));
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
                            var ping = Serializer.DeserializeWithLengthPrefix<Ping>(_ssl, PrefixStyle.Fixed32BigEndian);
                            _connection.ReceivePing(ping);
                            _protocol.Ping(ping);
                            break;
                        case PacketType.UserRemove:
                            _protocol.UserRemove(Serializer.DeserializeWithLengthPrefix<UserRemove>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.ChannelRemove:
                            _protocol.ChannelRemove(Serializer.DeserializeWithLengthPrefix<ChannelRemove>(_ssl, PrefixStyle.Fixed32BigEndian));
                            break;
                        case PacketType.TextMessage:
                            var message = Serializer.DeserializeWithLengthPrefix<TextMessage>(_ssl, PrefixStyle.Fixed32BigEndian);
                            _protocol.TextMessage(message);
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
