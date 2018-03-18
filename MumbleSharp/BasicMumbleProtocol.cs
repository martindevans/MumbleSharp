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
        private Thread _encodingThread;
        private UInt32 sequenceIndex;

        public bool IsEncodingThreadRunning { get; set; }

        public BasicMumbleProtocol()
        {
            
        }

        /// <summary>
        /// Associates this protocol with an opening mumble connection
        /// </summary>
        /// <param name="connection"></param>
        public virtual void Initialise(MumbleConnection connection)
        {
            Connection = connection;

            _encodingThread = new Thread(EncodingThreadEntry)
            {
                IsBackground = true
            };
        }
        public void Close()
        {
            _encodingThread.Abort();

            Connection = null;
            LocalUser = null;
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
        protected virtual void ChannelJoined(Channel channel)
        {
        }

        protected virtual void ChannelLeft(Channel channel)
        {
        }
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

            ChannelJoined(channel);

            Extensions.Log.Info("Chanel State", channelState);
        }

        /// <summary>
        /// Server has removed a channel
        /// </summary>
        /// <param name="channelRemove"></param>
        public virtual void ChannelRemove(ChannelRemove channelRemove)
        {
            Channel c;
            if (ChannelDictionary.TryRemove(channelRemove.ChannelId, out c))
            {
                ChannelLeft(c);
            }
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
            Extensions.Log.Info("User State", userState);

            if (userState.ShouldSerializeSession())
            {
                bool added = false;
                User user = UserDictionary.AddOrUpdate(userState.Session, i => {
                    added = true;
                    return new User(this, userState.Session);
                }, (i, u) => u);

                if (userState.ShouldSerializeSelfDeaf())
                    user.SelfDeaf = userState.SelfDeaf;
                if (userState.ShouldSerializeSelfMute())
                    user.SelfMuted = userState.SelfMute;
                if (userState.ShouldSerializeMute())
                    user.Muted = userState.Mute;
                if (userState.ShouldSerializeDeaf())
                    user.Deaf = userState.Deaf;
                if (userState.ShouldSerializeSuppress())
                    user.Suppress = userState.Suppress;
                if (userState.ShouldSerializeName())
                    user.Name = userState.Name;
                if (userState.ShouldSerializeComment())
                    user.Comment = userState.Comment;

                if (userState.ShouldSerializeChannelId())
                    user.Channel = ChannelDictionary[userState.ChannelId];
                else
                    user.Channel = RootChannel;

                //if (added)
                    UserJoined(user);
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
            LocalUser = UserDictionary[serverSync.Session];

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
                byte[] packet = null;
                try
                {
                    packet = _encodingBuffer.Encode(TransmissionCodec);
                }
                catch { }

                if (packet != null)
                {
                    int maxSize = 480;

                    //taken from JS port
                    for (int currentOffcet = 0; currentOffcet < packet.Length; )
                    {
                        int currentBlockSize = Math.Min(packet.Length - currentOffcet, maxSize);

                        byte type = TransmissionCodec == SpeechCodecs.Opus ? (byte)4 : (byte)0;
                        //originaly [type = codec_type_id << 5 | whistep_chanel_id]. now we can talk only to normal chanel
                        type = (byte)(type << 5);
                        byte[] sequence = Var64.writeVarint64_alternative((UInt64)sequenceIndex);

                        // Client side voice header.
                        byte[] voiceHeader = new byte[1 + sequence.Length];
                        voiceHeader[0] = type;
                        sequence.CopyTo(voiceHeader, 1);

                        byte[] header = Var64.writeVarint64_alternative((UInt64)currentBlockSize);
                        byte[] packedData = new byte[voiceHeader.Length + header.Length + currentBlockSize];

                        Array.Copy(voiceHeader, 0, packedData, 0, voiceHeader.Length);
                        Array.Copy(header, 0, packedData, voiceHeader.Length, header.Length);
                        Array.Copy(packet, currentOffcet, packedData, voiceHeader.Length + header.Length, currentBlockSize);

                        Connection.SendVoice(new ArraySegment<byte>(packedData));

                        sequenceIndex++;
                        currentOffcet += currentBlockSize;
                    }
                }

                //beware! can take a lot of power, because infinite loop without sleep
            }
        }

        public virtual void CodecVersion(CodecVersion codecVersion)
        {
            if (codecVersion.Opus)
                TransmissionCodec = SpeechCodecs.Opus;
            else if (codecVersion.PreferAlpha)
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
            sequenceIndex = 0;
        }
        #endregion

        

        
        /// <summary>
        /// Received a ping over the TCP connection
        /// </summary>
        /// <param name="ping"></param>
        public virtual void Ping(Ping ping)
        {
            
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

            if (textMessage.ChannelIds == null || textMessage.ChannelIds.Length == 0)
            {
                if (textMessage.TreeIds == null || textMessage.TreeIds.Length == 0)
                {
                    //personal message: no channel, no tree
                    PersonalMessageReceived(new PersonalMessage(user, string.Join("", textMessage.Message)));
                }
                else
                {
                    //recursive message: sent to multiple channels
                    Channel channel;
                    if (!ChannelDictionary.TryGetValue(textMessage.TreeIds[0], out channel))    //If we don't know the channel for this packet, just ignore it
                        return;

                    //TODO: This is a *tree* message - trace down the entire tree (using IDs in textMessage.TreeId as roots) and call ChannelMessageReceived for every channel
                    ChannelMessageReceived(new ChannelMessage(user, string.Join("", textMessage.Message), channel, true));
                }
            }
            else
            {
                foreach (uint channelId in textMessage.ChannelIds)
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
