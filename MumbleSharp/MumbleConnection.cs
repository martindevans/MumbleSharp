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
        private static double PING_DELAY_MILLISECONDS = 5000;

        public float? TcpPingAverage { get; set; }
        public float? TcpPingVariance { get; set; }
        public uint? TcpPingPackets { get; set; }

        public ConnectionStates State { get; private set; }

        TcpSocket _tcp;
        UdpSocket _udp;

        DateTime _lastSentPing = DateTime.MinValue;

        public IMumbleProtocol Protocol { get; private set; }

        /// <summary>
        /// Whether or not voice support is unabled with this connection
        /// </summary>
        public bool VoiceSupportEnabled { get; private set; }

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
        /// <param name="protocol">An object which will handle messages from the server.</param>
        /// <param name="voiceSupport">Whether or not voice support is unabled with this connection.</param>
        public MumbleConnection(string server, int port, IMumbleProtocol protocol, bool voiceSupport = true)
            : this(new IPEndPoint(Dns.GetHostAddresses(server).First(a => a.AddressFamily == AddressFamily.InterNetwork), port), protocol, voiceSupport)
        {
        }

        /// <summary>
        /// Creates a connection to the server
        /// </summary>
        /// <param name="host"></param>
        /// <param name="protocol"></param>
        /// <param name="voiceSupport">Whether or not voice support is unabled with this connection.</param>
        public MumbleConnection(IPEndPoint host, IMumbleProtocol protocol, bool voiceSupport = true)
        {
            Host = host;
            State = ConnectionStates.Disconnected;
            Protocol = protocol;
            VoiceSupportEnabled = voiceSupport;
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

            _udp?.Close();
            _tcp?.Close();

            State = ConnectionStates.Disconnected;
        }

        /// <summary>
        /// Processes a received network packet.
        /// This method should be called periodically.
        /// </summary>
        /// <returns>true, if a packet was processed. When this returns true you may want to recall the Process() method as soon as possible as their might be a queue on the network stack (like after a simple Thread.Yield() instead of a more relaxed Thread.Sleep(1) if it returned false).</returns>
        public bool Process()
        {
            if ((DateTime.UtcNow - _lastSentPing).TotalMilliseconds > PING_DELAY_MILLISECONDS)
            {
                _tcp.SendPing();

                if (_udp.IsConnected)
                    _udp.SendPing();

                _lastSentPing = DateTime.UtcNow;
            }

            _tcpProcessed = _tcp.Process();
            _udpProcessed = _udp.IsConnected ? _udp.Process() : false;
            return _tcpProcessed || _udpProcessed;
        }
        //declared outside method for alloc optimization
        private bool _tcpProcessed;
        private bool _udpProcessed;

        public void SendControl<T>(PacketType type, T packet)
        {
            _tcp.Send<T>(type, packet);
        }

        public void SendVoice(ArraySegment<byte> packet)
        {
            //The packet must be a well formed Mumble packet as described in https://mumble-protocol.readthedocs.org/en/latest/voice_data.html#packet-format
            //The packet is created in BasicMumbleProtocol's EncodingThread

            if (VoiceSupportEnabled)
                _tcp.SendVoice(PacketType.UDPTunnel, packet);
            else
                throw new InvalidOperationException("Voice Support is disabled with this connection");
        }

        internal void ReceivedEncryptedUdp(byte[] packet)
        {
            byte[] plaintext = _cryptState.Decrypt(packet, packet.Length);

            if (plaintext == null)
            {
                Console.WriteLine("Decryption failed");
                return;
            }

            ReceiveDecryptedUdp(plaintext);
        }

        internal void ReceiveDecryptedUdp(byte[] packet)
        {
            var type = packet[0] >> 5 & 0x7;

            if (type == 1)
                Protocol.UdpPing(packet);
            else if(VoiceSupportEnabled)
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

        /// <summary>
        /// Gets a value indicating whether ping stats should set timestamp when pinging.
        /// Only set the timestamp if we're currently connected.  This prevents the ping stats from being built.
        /// otherwise the stats will be throw off by the time it takes to connect.
        /// </summary>
        /// <value>
        ///   <c>true</c> if ping stats should set timestamp when pinging; otherwise, <c>false</c>.
        /// </value>
        internal bool ShouldSetTimestampWhenPinging { get; private set; }

        internal void ReceivePing(Ping ping)
        {
            ShouldSetTimestampWhenPinging = true;
            if (ping.ShouldSerializeTimestamp() && ping.Timestamp != 0)
            {
                var mostRecentPingtime =
                    (float)TimeSpan.FromTicks(DateTime.UtcNow.Ticks - (long)ping.Timestamp).TotalMilliseconds;

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
    }
}
