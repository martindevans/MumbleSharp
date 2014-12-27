using MumbleSharp.Audio.Codecs;
using MumbleSharp.Model;
using MumbleSharp.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MumbleSharp
{
    /// <summary>
    /// A basic mumble protocol which handles events from the server - override the individual handler methods to replace/extend the default behaviour
    /// </summary>
    public class BasicMumbleProtocol
        : IMumbleProtocol
    {
        public MumbleConnection Connection { get; private set; }

        protected readonly ConcurrentDictionary<UInt32, User> UserDictionary = new ConcurrentDictionary<UInt32, User>();
        public IEnumerable<User> Users
        {
            get { return UserDictionary.Values; }
        }

        protected readonly ConcurrentDictionary<UInt32, Channel> ChannelDictionary = new ConcurrentDictionary<UInt32, Channel>();
        public IEnumerable<Channel> Channels
        {
            get { return ChannelDictionary.Values; }
        }
        public Channel RootChannel { get; private set; }

        /// <summary>
        /// If true, this indicates that the connection was setup and the server accept this client
        /// </summary>
        public bool ReceivedServerSync { get; private set; }

        public User LocalUser { get; private set; }

        /// <summary>
        /// The current ping time (in seconds) for the TCP connection
        /// </summary>
        public virtual float TcpPing { get; private set; }

        /// <summary>
        /// Associates this protocol with an opening mumble connection
        /// </summary>
        /// <param name="connection"></param>
        public virtual void Initialise(MumbleConnection connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// Server has sent a version update
        /// </summary>
        /// <param name="version"></param>
        public virtual void Version(Packets.Version version)
        {
        }

        /// <summary>
        /// Validate the certificate the server sends for itself. By default this will acept *all* certificates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public virtual bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }

        public virtual void SuggestConfig(SuggestConfig config)
        {

        }

        #region Channels
        /// <summary>
        /// Server has changed some detail of a channel
        /// </summary>
        /// <param name="channelState"></param>
        public virtual void ChannelState(ChannelState channelState)
        {
            var channel = ChannelDictionary.AddOrUpdate(channelState.ChannelId, i => new Channel(this, channelState.ChannelId, channelState.Name, channelState.Parent) { Temporary = channelState.Temporary },
                (i, c) =>
                {
                    c.Name = channelState.Name;
                    return c;
                }
            );

            if (channel.Id == 0)
                RootChannel = channel;
        }

        /// <summary>
        /// Server has removed a channel
        /// </summary>
        /// <param name="channelRemove"></param>
        public virtual void ChannelRemove(ChannelRemove channelRemove)
        {
            Channel c;
            ChannelDictionary.TryRemove(channelRemove.ChannelId, out c);
        }
        #endregion

        #region users
        protected virtual void UserJoined(User user)
        {
        }

        protected virtual void UserLeft(User user)
        {
        }

        /// <summary>
        /// Server has changed some detail of a user
        /// </summary>
        /// <param name="userState"></param>
        public virtual void UserState(UserState userState)
        {
            if (userState.Session.HasValue)
            {
                bool added = false;
                User user = UserDictionary.AddOrUpdate(userState.Session.Value, i => {
                    added = true;
                    return new User(this, userState.Session.Value);
                }, (i, u) => u);

                if (added)
                    UserJoined(user);

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
                if (userState.Comment != null)
                    user.Comment = userState.Comment;

                if (userState.ChannelId.HasValue)
                    user.Channel = ChannelDictionary[userState.ChannelId.Value];
                else user.Channel = RootChannel;

                if (userState.Comment != null)
                    user.Comment = userState.Comment;
            }
        }

        /// <summary>
        /// A user has been removed from the server (left, kicked or banned)
        /// </summary>
        /// <param name="userRemove"></param>
        public virtual void UserRemove(UserRemove userRemove)
        {
            User user;
            if (UserDictionary.TryRemove(userRemove.Session, out user))
            {
                user.Channel = null;

                UserLeft(user);
            }

            if (user.Equals(LocalUser))
                Connection.Close();
        }
        #endregion

        public virtual void ContextAction(ContextAction contextAction)
        {
        }

        public virtual void ContextActionAdd(ContextActionAdd contextActionAdd)
        {
        }

        public virtual void PermissionQuery(PermissionQuery permissionQuery)
        {
        }

        #region server setup
        /// <summary>
        /// Initial connection to the server
        /// </summary>
        /// <param name="serverSync"></param>
        public virtual void ServerSync(ServerSync serverSync)
        {
            if (LocalUser != null)
                throw new InvalidOperationException("Second ServerSync Received");

            LocalUser = UserDictionary[serverSync.Session];

            ReceivedServerSync = true;
        }

        /// <summary>
        /// Some detail of the server configuration has changed
        /// </summary>
        /// <param name="serverConfig"></param>
        public virtual void ServerConfig(ServerConfig serverConfig)
        {
        }
        #endregion

        #region voice
        public virtual void CodecVersion(CodecVersion codecVersion)
        {
        }

        /// <summary>
        /// Get a voice decoder for the specified user/codec combination
        /// </summary>
        /// <param name="session"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        public virtual IVoiceCodec GetCodec(uint session, SpeechCodecs codec)
        {
            User user;
            if (!UserDictionary.TryGetValue(session, out user))
                return null;

            return user.GetCodec(codec);
        }

        /// <summary>
        /// Received a UDP ping from the server
        /// </summary>
        /// <param name="packet"></param>
        public virtual void UdpPing(byte[] packet)
        {
        }

        /// <summary>
        /// Received a voice packet from the server
        /// </summary>
        /// <param name="data"></param>
        /// <param name="userId"></param>
        /// <param name="sequence"></param>
        /// <param name="codec"></param>
        public virtual void EncodedVoice(byte[] data, uint userId, long sequence, IVoiceCodec codec)
        {
            User user;
            if (!UserDictionary.TryGetValue(userId, out user))
                return;

            user.EncodedVoice(data, sequence, codec);
        }
        #endregion

        /// <summary>
        /// Received a ping over the TCP connection
        /// </summary>
        /// <param name="ping"></param>
        public virtual void Ping(Ping ping)
        {
            TcpPing = ping.TcpPingAvg;
        }

        #region text messages
        /// <summary>
        /// Received a text message from the server
        /// </summary>
        /// <param name="textMessage"></param>
        public virtual void TextMessage(TextMessage textMessage)
        {
            User user;
            if (!UserDictionary.TryGetValue(textMessage.Actor, out user))   //If we don't know the user for this packet, just ignore it
                return;

            if (textMessage.ChannelId == null)
            {
                if (textMessage.TreeId == null)
                {
                    //personal message: no channel, no tree
                    PersonalMessageReceived(new PersonalMessage(user, string.Join("", textMessage.Message)));
                }
                else
                {
                    //recursive message: sent to multiple channels
                    Channel channel;
                    if (!ChannelDictionary.TryGetValue(textMessage.TreeId[0], out channel))    //If we don't know the channel for this packet, just ignore it
                        return;

                    //TODO: This is a *tree* message - trace down the entire tree (using IDs in textMessage.TreeId as roots) and call ChannelMessageReceived for every channel
                    ChannelMessageReceived(new ChannelMessage(user, string.Join("", textMessage.Message), channel, true));
                }
            }
            else
            {
                foreach (uint channelId in textMessage.ChannelId)
                {
                    Channel channel;
                    if (!ChannelDictionary.TryGetValue(channelId, out channel))
                        continue;

                    ChannelMessageReceived(new ChannelMessage(user, string.Join("", textMessage.Message), channel));
                }
                
            }
        }

        protected virtual void PersonalMessageReceived(PersonalMessage message)
        {
        }

        protected virtual void ChannelMessageReceived(ChannelMessage message)
        {
        }
        #endregion

        public virtual void UserList(UserList userList)
        {
        }

        public virtual X509Certificate SelectCertificate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return null;
        }
    }
}
