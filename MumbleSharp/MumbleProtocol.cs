using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using MumbleSharp.Model;
using System.Collections.Concurrent;

namespace MumbleSharp
{
    public class MumbleProtocol
        :IMumbleProtocol
    {
        MumbleConnection connection;

        ConcurrentDictionary<UInt32, User> users = new ConcurrentDictionary<UInt32, User>();
        ConcurrentDictionary<UInt32, Channel> channels = new ConcurrentDictionary<UInt32, Channel>();

        public User LocalUser { get; private set; }
        public Channel RootChannel { get; private set; }

        CryptState cryptState = new CryptState();

        public void Initialise(MumbleConnection connection)
        {
            this.connection = connection;
        }

        public void Version(Packets.Version version)
        {
        }

        #region Channels
        public void ChannelState(Packets.ChannelState channelState)
        {
            var channel = channels.AddOrUpdate(channelState.ChannelId, i =>
                {
                    return new Channel(channelState.ChannelId, channelState.Name, channelState.Parent) { Temporary = channelState.Temporary };
                },
                (i, c) =>
                {
                    c.Name = channelState.Name;
                    return c;
                }
            );

            if (channel.Id == 0)
                RootChannel = channel;
        }

        public void ChannelRemove(Packets.ChannelRemove channelRemove)
        {
            Channel c;
            channels.TryRemove(channelRemove.ChannelId, out c);
        }
        #endregion

        #region users
        public void UserState(Packets.UserState userState)
        {
            User user = users.AddOrUpdate(userState.Session.Value, i => { return new User(userState.Session.Value); }, (i, u) => u);

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
                user.Channel = channels[userState.ChannelId.Value];
        }

        public void UserRemove(Packets.UserRemove userRemove)
        {
            User user;
            if (users.TryRemove(userRemove.Session, out user))
                user.Dispose();
        }
        #endregion

        public void ContextAction(Packets.ContextAction contextAction)
        {
        }

        public void ContextActionAdd(Packets.ContextActionAdd contextActionAdd)
        {
        }

        public void PermissionQuery(Packets.PermissionQuery permissionQuery)
        {
        }

        #region server setup
        public void ServerSync(Packets.ServerSync serverSync)
        {
            if (LocalUser != null)
                throw new InvalidOperationException("Second ServerSync Received");

            LocalUser = users[serverSync.Session];
        }

        public void ServerConfig(Packets.ServerConfig serverConfig)
        {
        }
        #endregion

        #region voice
        public void CryptSetup(Packets.CryptSetup cryptSetup)
        {
            if (cryptSetup.Key != null && cryptSetup.ClientNonce != null && cryptSetup.ServerNonce != null) // Full key setup
            {
                cryptState.SetKeys(cryptSetup.Key, cryptSetup.ClientNonce, cryptSetup.ServerNonce);
            }
            else if (cryptSetup.ServerNonce != null) // Server syncing its nonce to us.
            {
                cryptState.ServerNonce = cryptSetup.ServerNonce;
            }
            else // Server wants our nonce.
            {
                connection.SendControl<Packets.CryptSetup>(Packets.PacketType.CryptSetup, new Packets.CryptSetup() { ClientNonce = cryptState.ClientNonce });
            }
        }

        public void CodecVersion(Packets.CodecVersion codecVersion)
        {
        }

        public void UdpTunnel(Packets.UdpTunnel udpTunnel)
        {
            ProcessUdpPacket(udpTunnel.Packet);
        }

        public void Udp(byte[] packet)
        {
            byte[] plaintext = cryptState.Decrypt(packet, packet.Length);

            if (plaintext != null)
                ProcessUdpPacket(plaintext);
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
            int type = packet[0] >> 5 & 0x7;
            int flags = packet[0] & 0x1f;
        }
        #endregion

        public void Ping(Packets.Ping ping)
        {
        }

        public virtual void TextMessage(Packets.TextMessage textMessage)
        {
        }
    }
}
