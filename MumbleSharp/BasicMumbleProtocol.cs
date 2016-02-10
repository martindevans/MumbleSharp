using System.Threading;
using MumbleProto;
using MumbleSharp.Audio;
using MumbleSharp.Audio.Codecs;
using MumbleSharp.Model;
using MumbleSharp.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Version = MumbleProto.Version;

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
            get
            {
                return UserDictionary.Values;
            }
        }

        protected readonly ConcurrentDictionary<UInt32, Channel> ChannelDictionary = new ConcurrentDictionary<UInt32, Channel>();

        public IEnumerable<Channel> Channels
        {
            get
            {
                return ChannelDictionary.Values;
            }
        }

        public Channel RootChannel { get; private set; }

        /// <summary>
        /// If true, this indicates that the connection was setup and the server accept this client
        /// </summary>
        public bool ReceivedServerSync { get; private set; }

        public SpeechCodecs TransmissionCodec { get; private set; }

        public User LocalUser { get; private set; }

        private AudioEncodingBuffer _encodingBuffer;
        private readonly Thread _encodingThread;

        public bool IsEncodingThreadRunning { get; set; }

        public BasicMumbleProtocol()
        {
            _encodingThread = new Thread(EncodingThreadEntry) {
                IsBackground = true
            };
        }

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
        public virtual void Version(Version version)
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
            var channel = ChannelDictionary.AddOrUpdate(channelState.channel_id, i => new Channel(this, channelState.channel_id, channelState.name, channelState.parent) { Temporary = channelState.temporary },
                (i, c) =>
                {
                    c.Name = channelState.name;
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
            ChannelDictionary.TryRemove(channelRemove.channel_id, out c);
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
            if (userState.sessionSpecified)
            {
                bool added = false;
                User user = UserDictionary.AddOrUpdate(userState.session, i => {
                    added = true;
                    return new User(this, userState.session);
                }, (i, u) => u);

                if (added)
                    UserJoined(user);

                if (userState.self_deafSpecified)
                    user.Deaf = userState.self_deaf;
                if (userState.self_muteSpecified)
                    user.Muted = userState.self_mute;
                if (userState.muteSpecified)
                    user.Muted = userState.mute;
                if (userState.deafSpecified)
                    user.Deaf = userState.deaf;
                if (userState.suppressSpecified)
                    user.Muted = userState.suppress;
                if (userState.nameSpecified)
                    user.Name = userState.name;
                if (userState.commentSpecified)
                    user.Comment = userState.comment;

                if (userState.channel_idSpecified)
                    user.Channel = ChannelDictionary[userState.channel_id];
                else
                    user.Channel = RootChannel;
            }
        }

        /// <summary>
        /// A user has been removed from the server (left, kicked or banned)
        /// </summary>
        /// <param name="userRemove"></param>
        public virtual void UserRemove(UserRemove userRemove)
        {
            User user;
            if (UserDictionary.TryRemove(userRemove.session, out user))
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

        public virtual void ContextActionModify(ContextActionModify contextActionModify)
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

            //Get the local user
            LocalUser = UserDictionary[serverSync.session];

            _encodingBuffer = new AudioEncodingBuffer();
            _encodingThread.Start();

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
        private void EncodingThreadEntry()
        {
            IsEncodingThreadRunning = true;
            while (IsEncodingThreadRunning)
            {
                var packet = _encodingBuffer.Encode(TransmissionCodec);
                if (packet != null)
                    Connection.SendVoice(new ArraySegment<byte>(packet));
            }
        }

        public virtual void CodecVersion(CodecVersion codecVersion)
        {
            if (codecVersion.opus)
                TransmissionCodec = SpeechCodecs.Opus;
            else if (codecVersion.prefer_alpha)
                TransmissionCodec = SpeechCodecs.CeltAlpha;
            else
                TransmissionCodec = SpeechCodecs.CeltBeta;
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
        /// <param name="target"></param>
        public virtual void EncodedVoice(byte[] data, uint userId, long sequence, IVoiceCodec codec, SpeechTarget target)
        {
            User user;
            if (!UserDictionary.TryGetValue(userId, out user))
                return;

            user.ReceiveEncodedVoice(data, sequence, codec);
        }

        public void SendVoice(ArraySegment<byte> pcm, SpeechTarget target, uint targetId)
        {
            _encodingBuffer.Add(pcm, target, targetId);
        }

        public void SendVoiceStop()
        {
            _encodingBuffer.Stop();
        }
        #endregion

        //using the approch described here to do running calculations of ping values.
        // http://dsp.stackexchange.com/questions/811/determining-the-mean-and-standard-deviation-in-real-time
        float _meanOfPings;
        float _varianceTimesCountOfPings;
        int _countOfPings;

        
        /// <summary>
        /// Received a ping over the TCP connection
        /// </summary>
        /// <param name="ping"></param>
        public virtual void Ping(Ping ping)
        {
            Connection.ShouldSetTimestampWhenPinging = true;
            if (ping.timestampSpecified && ping.timestamp != 0)
            {
                var mostRecentPingtime =
                    (float) TimeSpan.FromTicks(DateTime.Now.Ticks - (long) ping.timestamp).TotalMilliseconds;
                
                //The ping time is the one-way transit time.
                mostRecentPingtime /= 2;

                var previousMean = _meanOfPings;
                _countOfPings++;
                _meanOfPings = _meanOfPings + ((mostRecentPingtime - _meanOfPings)/_countOfPings);
                _varianceTimesCountOfPings = _varianceTimesCountOfPings +
                                             ((mostRecentPingtime - _meanOfPings)*(mostRecentPingtime - previousMean));

                Connection.TcpPingPackets = (uint) _countOfPings;
                Connection.TcpPingAverage = _meanOfPings;
                Connection.TcpPingVariance = _varianceTimesCountOfPings/_countOfPings;
            }
        }

        #region text messages
        /// <summary>
        /// Received a text message from the server
        /// </summary>
        /// <param name="textMessage"></param>
        public virtual void TextMessage(TextMessage textMessage)
        {
            User user;
            if (!UserDictionary.TryGetValue(textMessage.actor, out user))   //If we don't know the user for this packet, just ignore it
                return;

            if (textMessage.channel_id == null)
            {
                if (textMessage.tree_id == null)
                {
                    //personal message: no channel, no tree
                    PersonalMessageReceived(new PersonalMessage(user, string.Join("", textMessage.message)));
                }
                else
                {
                    //recursive message: sent to multiple channels
                    Channel channel;
                    if (!ChannelDictionary.TryGetValue(textMessage.tree_id[0], out channel))    //If we don't know the channel for this packet, just ignore it
                        return;

                    //TODO: This is a *tree* message - trace down the entire tree (using IDs in textMessage.TreeId as roots) and call ChannelMessageReceived for every channel
                    ChannelMessageReceived(new ChannelMessage(user, string.Join("", textMessage.message), channel, true));
                }
            }
            else
            {
                foreach (uint channelId in textMessage.channel_id)
                {
                    Channel channel;
                    if (!ChannelDictionary.TryGetValue(channelId, out channel))
                        continue;

                    ChannelMessageReceived(new ChannelMessage(user, string.Join("", textMessage.message), channel));
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
