using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MumbleSharp;
using MumbleSharp.Model;

namespace MumbleClient
{
    /// <summary>
    /// A test mumble protocol.
    /// Currently just prints out text messages in the root channel. Voice data isn't decoded!
    /// </summary>
    public class MumbleProtocol
        :IMumbleProtocol
    {
        MumbleConnection _connection;

        readonly ConcurrentDictionary<UInt32, User> _users = new ConcurrentDictionary<UInt32, User>();
        public IEnumerable<User> Users
        {
            get { return _users.Values; }
        }

        readonly ConcurrentDictionary<UInt32, Channel> _channels = new ConcurrentDictionary<UInt32, Channel>();
        public IEnumerable<Channel> Channels
        {
            get { return _channels.Values; }
        }

        public User LocalUser { get; private set; }
        public Channel RootChannel { get; private set; }

        public void Initialise(MumbleConnection connection)
        {
            _connection = connection;
        }

        public void Version(MumbleSharp.Packets.Version version)
        {
        }

        public bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }

        #region Channels
        public void ChannelState(MumbleSharp.Packets.ChannelState channelState)
        {
            var channel = _channels.AddOrUpdate(channelState.ChannelId, i => new Channel(channelState.ChannelId, channelState.Name, channelState.Parent) { Temporary = channelState.Temporary },
                (i, c) =>
                {
                    c.Name = channelState.Name;
                    return c;
                }
            );

            if (channel.Id == 0)
                RootChannel = channel;
        }

        public void ChannelRemove(MumbleSharp.Packets.ChannelRemove channelRemove)
        {
            Channel c;
            _channels.TryRemove(channelRemove.ChannelId, out c);
        }
        #endregion

        #region users
        public void UserState(MumbleSharp.Packets.UserState userState)
        {
            if (userState.Session.HasValue)
            {
                User user = _users.AddOrUpdate(userState.Session.Value, i => new User(userState.Session.Value), (i, u) => u);

                if (userState.SelfDeaf.HasValue)
                    user.Deaf = userState.SelfDeaf.Value;
                if (userState.SelfMute.HasValue)
                    user.Muted = userState.SelfMute.Value;
                if (userState.Mute.HasValue)
                    user.Muted = userState.Mute.Value;
                if (userState.Deaf.HasValue)
                    user.Deaf = userState.Deaf.Value;
                if (userState.Suppress.HasValue)
                    user.Muted = userState.Suppress.Value;
                if (userState.Name != null)
                    user.Name = userState.Name;
                if (userState.ChannelId.HasValue)
                    user.Channel = _channels[userState.ChannelId.Value];
            }
        }

        public void UserRemove(MumbleSharp.Packets.UserRemove userRemove)
        {
            User user;
            if (_users.TryRemove(userRemove.Session, out user))
                user.Dispose();
			if (user.Equals(LocalUser))
			{
				//Console.WriteLine(((userRemove.Ban) ? "Banned" : "Kicked") + " from server. Reason: " + userRemove.Reason);
				_connection.Close();
			}
        }
        #endregion

        public void ContextAction(MumbleSharp.Packets.ContextAction contextAction)
        {
        }

        public void ContextActionAdd(MumbleSharp.Packets.ContextActionAdd contextActionAdd)
        {
        }

        public void PermissionQuery(MumbleSharp.Packets.PermissionQuery permissionQuery)
        {
        }

        #region server setup
        public void ServerSync(MumbleSharp.Packets.ServerSync serverSync)
        {
            if (LocalUser != null)
                throw new InvalidOperationException("Second ServerSync Received");

            LocalUser = _users[serverSync.Session];
        }

        public void ServerConfig(MumbleSharp.Packets.ServerConfig serverConfig)
        {
        }
        #endregion

        #region voice
        public void CodecVersion(MumbleSharp.Packets.CodecVersion codecVersion)
        {
        }

        public void Udp(byte[] packet)
        {
            if (packet != null)
                ProcessUdpPacket(packet);
        }

        private void ProcessUdpPacket(byte[] packet)
        {
            int type = packet[0] >> 5 & 0x7;

            if (type == 1)
                UdpPing(packet);
            else
                Voice(packet);
        }

        private void UdpPing(byte[] packet)
        {
        }

        private void Voice(byte[] packet)
        {
            //This is all horrible!
            //TODO: Move all the decoding related rubbish to within the MumbleConnection (internal void ReceivedEncryptedUdp(byte[] packet) Line 78)
            //This should just receive ordered blocks of decoded PCM data 

            int type = packet[0] >> 5 & 0x7;
            int flags = packet[0] & 0x1f;

            if (type != (int)SpeechCodecs.CeltAlpha && type != (int)SpeechCodecs.CeltBeta && type != (int)SpeechCodecs.Speex)
                return;

            using (var reader = new UdpPacketReader(new MemoryStream(packet, 1, packet.Length - 1)))
            {
                Int64 session = reader.ReadVarInt64();
                User user;
                if (!_users.TryGetValue((uint)session, out user))   //If we don't know the user for this packet, just ignore the packet
                    return;

                Int64 sequence = reader.ReadVarInt64();

                byte header = 0;
                do
                {
                    header = reader.ReadByte();
                    int length = header & 127;
                    byte[] data = reader.ReadBytes(length);

                    //TODO:: We have a voice packet for this user (data)... do something with it!

                } while ((header & 128) == 0);
            }
        }
        #endregion

        public void Ping(MumbleSharp.Packets.Ping ping)
        {
        }

        public void TextMessage(MumbleSharp.Packets.TextMessage textMessage)
        {
            User user;
            if (!_users.TryGetValue(textMessage.Actor, out user))   //If we don't know the user for this packet, just ignore it
                return;

            Channel c;
            if (!_channels.TryGetValue(textMessage.ChannelId[0], out c))    //If we don't know the channel for this packet, just ignore it
                return;

            for (int i = 0; i < textMessage.Message.Length; i++)
                Console.WriteLine(user.Name + " (" + c.Name + "): " + textMessage.Message[i]);
        }

        public void UserList(MumbleSharp.Packets.UserList userList)
        {
        }


        public X509Certificate SelectCertificate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return null;
        }
    }
}
