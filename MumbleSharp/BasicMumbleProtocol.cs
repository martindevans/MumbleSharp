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
using static MumbleSharp.Audio.AudioEncodingBuffer;

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
        private ThreadStart _encodingThreadStart;
        private Thread _encodingThread;
        private Exception _encodingThreadException;
        private UInt32 sequenceIndex;

        public bool IsEncodingThreadRunning { get; set; }

        private int _audioSampleRate;
        private byte _audioSampleBits;
        private byte _audioSampleChannels;
        private ushort _audioFrameSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicMumbleProtocol"/> class.
        /// </summary>
        /// <param name="audioSampleRate">The sample rate in Hertz (samples per second).</param>
        /// <param name="audioSampleBits">The sample bit depth.</param>
        /// <param name="audioSampleChannels">The sample channels (1 for mono, 2 for stereo).</param>
        /// <param name="audioFrameSize">Size of the frame in samples.</param>
        public BasicMumbleProtocol(int audioSampleRate = Constants.DEFAULT_AUDIO_SAMPLE_RATE, byte audioSampleBits = Constants.DEFAULT_AUDIO_SAMPLE_BITS, byte audioSampleChannels = Constants.DEFAULT_AUDIO_SAMPLE_CHANNELS, ushort audioFrameSize = Constants.DEFAULT_AUDIO_FRAME_SIZE)
        {
            _audioSampleRate = audioSampleRate;
            _audioSampleBits = audioSampleBits;
            _audioSampleChannels = audioSampleChannels;
            _audioFrameSize = audioFrameSize;
        }

        /// <summary>
        /// Associates this protocol with an opening mumble connection
        /// </summary>
        /// <param name="connection"></param>
        public virtual void Initialise(MumbleConnection connection)
        {
            Connection = connection;

            if (connection.VoiceSupportEnabled)
            {
                //Start the EncodingThreadEntry thread, and collect a possible exception at termination
                _encodingThreadStart = new ThreadStart(() => EncodingThreadEntry(out _encodingThreadException));
                _encodingThreadStart += () =>
                {
                    if (_encodingThreadException != null)
                        throw new Exception($"{nameof(BasicMumbleProtocol)}'s {nameof(_encodingThread)} was terminated unexpectedly because of a {_encodingThreadException.GetType().ToString()}", _encodingThreadException);
                };

                _encodingThread = new Thread(_encodingThreadStart)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.AboveNormal
                };
            }
        }
        public void Close()
        {
            if (Connection != null && _encodingThread != null
                && Connection.VoiceSupportEnabled)
            {
                //request encoding thread to exit
                IsEncodingThreadRunning = false;
                //wait until thread has exited gracefuly (1s max)
                _encodingThread.Join(1000);
            }

            Connection = null;

            LocalUser = null;
        }

        /// <summary>
        /// Server has sent a version update.
        /// </summary>
        /// <param name="version"></param>
        public virtual void Version(Version version)
        {
            
        }

        /// <summary>
        /// Validate the certificate the server sends for itself. By default this will acept *all* certificates.
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

        /// <summary>
        /// Sent by the server to inform the clients of suggested client configuration specified by the server administrator.
        /// </summary>
        /// <param name="config"></param>
        public virtual void SuggestConfig(SuggestConfig config)
        {

        }

        #region Channels
        protected void SendChannelCreate(Channel channel)
        {
            if (channel.Id != 0)
                throw new ArgumentException("For channel creation the ChannelId cannot be forced, use 0 to avoid this error.", nameof(channel));

            SendChannelState(new MumbleProto.ChannelState()
            {
                //ChannelId = channel.Id, //for channel creation the ChannelId must not be set
                Parent = channel.Parent,
                Position = channel.Position,
                Name = channel.Name,
                Description = channel.Description,
                Temporary = channel.Temporary,
                //MaxUsers = 0, //If this value is zero, the maximum number of users allowed in the channel is given by the server's "usersperchannel" setting.
            });
        }
        protected void SendChannelMove(Channel channel, uint parentChannelId)
        {
            SendChannelState(new MumbleProto.ChannelState()
            {
                ChannelId = channel.Id,
                Parent = parentChannelId,
            });
        }

        protected virtual void ChannelJoined(Channel channel)
        {
        }

        protected virtual void ChannelLeft(Channel channel)
        {
        }

        /// <summary>
        /// Used to communicate channel properties between the client and the server.
        /// Sent by the server during the login process or when channel properties are updated.
        /// </summary>
        /// <param name="channelState"></param>
        public virtual void ChannelState(ChannelState channelState)
        {
            if(!channelState.ShouldSerializeChannelId())
                throw new InvalidOperationException($"{nameof(ChannelState)} must provide a {channelState.ChannelId}.");

            var channel = ChannelDictionary.AddOrUpdate(channelState.ChannelId, i =>
                {
                    //Add new channel to the dictionary
                    return new Channel(this, channelState.ChannelId, channelState.Name, channelState.Parent)
                    {
                        Temporary = channelState.Temporary,
                        Description = channelState.Description,
                        Position = channelState.Position
                    };
                },
                (i, c) => c);

            //Update channel in the dictionary
            if (channelState.ShouldSerializeName())
                channel.Name = channelState.Name;
            if (channelState.ShouldSerializeParent())
                channel.Parent = channelState.Parent;
            if (channelState.ShouldSerializeTemporary())
                channel.Temporary = channelState.Temporary;
            if (channelState.ShouldSerializeDescription())
                channel.Description = channelState.Description;
            if (channelState.ShouldSerializePosition())
                channel.Position = channelState.Position;

            if (channel.Id == 0)
                RootChannel = channel;

            ChannelJoined(channel);

            Extensions.Log.Info("Chanel State", channelState);
        }

        /// <summary>
        /// Used to communicate channel properties between the client and the server.
        /// Client may use this message to update said channel properties.
        /// </summary>
        /// <param name="channelState"></param>
        public void SendChannelState(ChannelState channelState)
        {
            Connection.SendControl(PacketType.ChannelState, channelState);
        }

        /// <summary>
        /// Sent by the server when a channel has been removed and clients should be notified.
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

        /// <summary>
        /// Sent by the client when it wants a channel removed.
        /// </summary>
        /// <param name="channelRemove"></param>
        public void SendChannelRemove(ChannelRemove channelRemove)
        {
            Connection.SendControl(PacketType.ChannelRemove, channelRemove);
        }
        #endregion

        #region users
        /// <summary>
        /// Called when the user has joined.
        /// </summary>
        /// <param name="user">the user who joined</param>
        protected virtual void UserJoined(User user)
        {
        }

        /// <summary>
        /// Called when any of the user's state properties has changed.
        /// For precise state management you may want to use UserStateDeafChanged, UserStateMuteChanged, UserStateNameChanged, UserStateCommentChanged or UserStateChannelChanged method overrides.
        /// </summary>
        /// <param name="user">the user who's tate has changed</param>
        protected virtual void UserStateChanged(User user)
        {
        }

        /// <summary>
        /// Called if user's Deaf or SelfDeaf state have changed
        /// </summary>
        /// <param name="user">the user who's state has changed</param>
        /// <param name="oldSelfDeafValue">the previous value of the user's SelfDeaf state</param>
        /// <param name="oldDeafValue">the previous value of the user's Deaf state</param>
        protected virtual void UserStateDeafChanged(User user, bool oldSelfDeafValue, bool oldDeafValue)
        {
        }

        /// <summary>
        /// Called if user's Mute or SelfMuted or Supress state have changed
        /// </summary>
        /// <param name="user">the user who's state has changed</param>
        /// <param name="oldSelfMutedValue">the previous value of the user's SelfMuted state</param>
        /// <param name="oldMutedValue">the previous value of the user's Muted state</param>
        /// <param name="oldSuppressValue">the previous value of the user's Suppress state</param>
        protected virtual void UserStateMutedChanged(User user, bool oldSelfMutedValue, bool oldMutedValue, bool oldSuppressValue)
        {
        }

        /// <summary>
        /// Called if user's name has changed
        /// </summary>
        /// <param name="user">the user who's state has changed</param>
        /// <param name="oldName">the previous value of the uer's name</param>
        protected virtual void UserStateNameChanged(User user, string oldName)
        {
        }

        /// <summary>
        /// Called if user's comment has changed
        /// </summary>
        /// <param name="user">the user who's state has changed</param>
        /// <param name="oldComment">the previous value of the uer's comment</param>
        protected virtual void UserStateCommentChanged(User user, string oldComment)
        {
        }

        /// <summary>
        /// Called if user's channel has changed
        /// </summary>
        /// <param name="user">the user who's state has changed</param>
        /// <param name="oldChannelId">the preivous value of user's channel id</param>
        protected virtual void UserStateChannelChanged(User user, uint oldChannelId)
        {
        }

        /// <summary>
        /// Called when the user has left
        /// </summary>
        /// <param name="user">the user who left</param>
        protected virtual void UserLeft(User user)
        {
        }

        /// <summary>
        /// Sent by the server when it communicates new and changed users to client.
        /// First seen during login procedure.
        /// </summary>
        /// <param name="userState"></param>
        public virtual void UserState(UserState userState)
        {
            Extensions.Log.Info("User State", userState);

            if (userState.ShouldSerializeSession())
            {
                bool added = false;
                User user = UserDictionary.AddOrUpdate(userState.Session, i =>
                {
                    //Add new user to the dictionary
                    added = true;
                    return new User(this, userState.Session, _audioSampleRate, _audioSampleBits, _audioSampleChannels);
                }, (i, u) => u);

                bool triggerUserStateMutedChanged = false;
                bool oldSelfMuted = false;
                bool oldMuted = false;
                bool oldSuppress = false;
                bool triggerUserStateDeafChanged = false;
                bool oldSelfDeaf = false;
                bool oldDeaf = false;
                bool triggerUserStateNameChanged = false;
                string oldName = string.Empty;
                bool triggerUserStateCommentChanged = false;
                string oldComment = string.Empty;
                bool triggerUserStateChannelChanged = false;
                uint oldChannelId = RootChannel.Id;

                //Update user in the dictionary
                if (userState.ShouldSerializeSelfDeaf())
                {
                    oldSelfDeaf = user.SelfDeaf;
                    user.SelfDeaf = userState.SelfDeaf;
                    triggerUserStateDeafChanged = true;
                }
                if (userState.ShouldSerializeSelfMute())
                {
                    oldSelfMuted = user.SelfMuted;
                    user.SelfMuted = userState.SelfMute;
                    triggerUserStateMutedChanged = true;
                }
                if (userState.ShouldSerializeMute())
                {
                    oldMuted = user.Muted;
                    user.Muted = userState.Mute;
                    triggerUserStateMutedChanged = true;
                }
                if (userState.ShouldSerializeDeaf())
                {
                    oldDeaf = user.Deaf;
                    user.Deaf = userState.Deaf;
                    triggerUserStateDeafChanged = true;
                }
                if (userState.ShouldSerializeSuppress())
                {
                    oldSuppress = user.Suppress;
                    user.Suppress = userState.Suppress;
                    triggerUserStateMutedChanged = true;
                }
                if (userState.ShouldSerializeName())
                {
                    oldName = user.Name != null ? string.Copy(user.Name) : null;
                    user.Name = userState.Name;
                    triggerUserStateNameChanged = true;
                }
                if (userState.ShouldSerializeComment())
                {
                    oldComment = user.Comment != null ? string.Copy(user.Comment) : null;
                    user.Comment = userState.Comment;
                    triggerUserStateCommentChanged = true;
                }

                if (userState.ShouldSerializeChannelId())
                {
                    if (user.Channel != null)
                        oldChannelId = user.Channel.Id;
                    else
                        oldChannelId = RootChannel.Id;
                    user.Channel = ChannelDictionary[userState.ChannelId];
                    triggerUserStateChannelChanged = true;
                }
                else if (user.Channel == null)
                {
                    oldChannelId = RootChannel.Id;
                    user.Channel = RootChannel;
                    triggerUserStateChannelChanged = true;
                }

                if (added)
                    UserJoined(user);
                else
                {
                    if (triggerUserStateDeafChanged)
                        UserStateDeafChanged(user, oldSelfDeaf, oldDeaf);

                    if (triggerUserStateMutedChanged)
                        UserStateMutedChanged(user, oldSelfMuted, oldMuted, oldSuppress);

                    if (triggerUserStateNameChanged)
                        UserStateNameChanged(user, oldName);

                    if (triggerUserStateCommentChanged)
                        UserStateCommentChanged(user, oldComment);

                    if (triggerUserStateChannelChanged)
                        UserStateChannelChanged(user, oldChannelId);

                    UserStateChanged(user);
                }
            }
        }

        /// <summary>
        /// Sent by the client when it wishes to alter its state.
        /// </summary>
        /// <param name="userState"></param>
        public void SendUserState(UserState userState)
        {
            Connection.SendControl(PacketType.UserState, userState);
        }

        /// <summary>
        /// Used to communicate user leaving or being kicked.
        /// Sent by the server when it informs the clients that a user is not present anymore.
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

        /// <summary>
        /// Sent by the client when it attempts to kick a user.
        /// </summary>
        /// <param name="userRemove"></param>
        public void SendUserRemove(UserRemove userRemove)
        {
            Connection.SendControl(PacketType.UserRemove, userRemove);
        }
        #endregion

        public virtual void ContextAction(ContextAction contextAction)
        {
        }

        /// <summary>
        /// Sent by the client when it wants to initiate a Context action.
        /// </summary>
        /// <param name="contextActionModify"></param>
        public void SendContextActionModify(ContextActionModify contextActionModify)
        {
            Connection.SendControl(PacketType.ContextActionModify, contextActionModify);
        }

        #region permissions
        /// <summary>
        /// Sent by the server when it replies to the query or wants the user to resync all channel permissions.
        /// </summary>
        /// <param name="permissionQuery"></param>
        public virtual void PermissionQuery(PermissionQuery permissionQuery)
        {
            if (permissionQuery.Flush)
            {
                foreach (var channel in ChannelDictionary.Values)
                {
                    channel.Permissions = 0; // Permissions.DEFAULT_PERMISSIONS;
                }
            }
            else if (permissionQuery.ShouldSerializeChannelId())
            {
                Channel channel;
                if (!ChannelDictionary.TryGetValue(permissionQuery.ChannelId, out channel))
                    throw new InvalidOperationException($"{nameof(PermissionQuery)} provided an unknown {permissionQuery.ChannelId}.");

                if (permissionQuery.ShouldSerializePermissions())
                    channel.Permissions = (Permission)permissionQuery.Permissions;
            }
            else
            {
                throw new InvalidOperationException($"{nameof(PermissionQuery)} must provide either {nameof(permissionQuery.Flush)} or {nameof(permissionQuery.ChannelId)}.");
            }
        }

        /// <summary>
        /// Sent by the client when it wants permissions for a certain channel.
        /// </summary>
        /// <param name="permissionQuery"></param>
        public void SendPermissionQuery(PermissionQuery permissionQuery)
        {
            Connection.SendControl(PacketType.PermissionQuery, permissionQuery);
        }

        /// <summary>
        /// Sent by the server when it rejects the user connection.
        /// </summary>
        /// <param name="reject"></param>
        public virtual void Reject(Reject reject)
        {
        }

        public virtual void PermissionDenied(PermissionDenied permissionDenied)
        {
        }

        /// <summary>
        /// Used by the client to send the authentication credentials to the server.
        /// </summary>
        /// <param name="authenticate"></param>
        public void SendAuthenticate(Authenticate authenticate)
        {
            Connection.SendControl(PacketType.Authenticate, authenticate);
        }

        public virtual void Acl(Acl acl)
        {
        }
        #endregion

        #region server setup
        /// <summary>
        /// ServerSync message is sent by the server when it has authenticated the user and finished synchronizing the server state.
        /// </summary>
        /// <param name="serverSync"></param>
        public virtual void ServerSync(ServerSync serverSync)
        {
            if (LocalUser != null)
                throw new InvalidOperationException("Second ServerSync Received");

            if (!serverSync.ShouldSerializeSession())
                throw new InvalidOperationException($"{nameof(ServerSync)} must provide a {nameof(serverSync.Session)}.");

            //Get the local user
            LocalUser = UserDictionary[serverSync.Session];

            //TODO: handle the serverSync.WelcomeText, serverSync.Permissions, serverSync.MaxBandwidth

            if (Connection.VoiceSupportEnabled)
            {
                _encodingBuffer = new AudioEncodingBuffer(_audioSampleRate, _audioSampleBits, _audioSampleChannels, _audioFrameSize);
                _encodingThread.Start();
            }

            ReceivedServerSync = true;
        }

        /// <summary>
        /// Sent by the server when it informs the clients on server configuration details.
        /// </summary>
        /// <param name="serverConfig"></param>
        public virtual void ServerConfig(ServerConfig serverConfig)
        {
            
        }
        #endregion

        #region voice
        private void EncodingThreadEntry(out Exception exception)
        {
            if(!Connection.VoiceSupportEnabled)
                throw new InvalidOperationException("Voice Support is disabled with this connection");

            exception = null;
            IsEncodingThreadRunning = true;
            try
            {
                while (IsEncodingThreadRunning)
                {
                    EncodedTargettedSpeech? encodedTargettedSpeech = _encodingBuffer.Encode(TransmissionCodec);

                    if (encodedTargettedSpeech.HasValue)
                    {
                        int maxSize = 480;

                        //taken from JS port
                        for (int currentOffset = 0; currentOffset < encodedTargettedSpeech.Value.EncodedPcm.Length;)
                        {
                            int currentBlockSize = Math.Min(encodedTargettedSpeech.Value.EncodedPcm.Length - currentOffset, maxSize);

                            byte type = TransmissionCodec == SpeechCodecs.Opus ? (byte)4 : (byte)0;
                            //originaly [type = codec_type_id << 5 | whistep_chanel_id].
                            var typeTarget = (byte)(type << 5 | (int)encodedTargettedSpeech.Value.Target);
                            byte[] sequence = Var64.writeVarint64_alternative((UInt64)sequenceIndex);

                            // Client side voice header.
                            byte[] voiceHeader = new byte[1 + sequence.Length];
                            voiceHeader[0] = typeTarget;
                            sequence.CopyTo(voiceHeader, 1);

                            byte[] header = Var64.writeVarint64_alternative((UInt64)currentBlockSize);
                            byte[] packedData = new byte[voiceHeader.Length + header.Length + currentBlockSize];

                            Array.Copy(voiceHeader, 0, packedData, 0, voiceHeader.Length);
                            Array.Copy(header, 0, packedData, voiceHeader.Length, header.Length);
                            Array.Copy(encodedTargettedSpeech.Value.EncodedPcm, currentOffset, packedData, voiceHeader.Length + header.Length, currentBlockSize);

                            Connection?.SendVoice(new ArraySegment<byte>(packedData));

                            sequenceIndex++;
                            currentOffset += currentBlockSize;
                        }
                    }
                    else
                    {
                        Thread.Sleep(1); //avoids consuming a cpu core at 100% if there's nothing to encode...
                    }
                }
            }
            catch (Exception ex)
            {
                exception = ex;
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
            if (!Connection.VoiceSupportEnabled)
                throw new InvalidOperationException("Voice Support is disabled with this connection");

            User user;
            if (!UserDictionary.TryGetValue(userId, out user))
                return;

            user.ReceiveEncodedVoice(data, sequence, codec);
        }

        public void SendVoice(ArraySegment<byte> pcm, SpeechTarget target, uint targetId)
        {
            if (!Connection.VoiceSupportEnabled)
                throw new InvalidOperationException("Voice Support is disabled with this connection");

            _encodingBuffer.Add(pcm, target, targetId);
        }

        public void SendVoiceStop()
        {
            if (!Connection.VoiceSupportEnabled)
                throw new InvalidOperationException("Voice Support is disabled with this connection");

            _encodingBuffer.Stop();
            sequenceIndex = 0;
        }
        #endregion

        #region ping
        /// <summary>
        /// Received a ping over the TCP connection.
        /// Server must reply to the client Ping packet with the same timestamp and its own good/late/lost/resync numbers. None of the fields is strictly required.
        /// </summary>
        /// <param name="ping"></param>
        public virtual void Ping(Ping ping)
        {
            
        }

        /// <summary>
        /// Sent by the client to notify the server that the client is still alive.
        /// </summary>
        /// <param name="ping"></param>
        public void SendPing(Ping ping)
        {
            Connection.SendControl(PacketType.Ping, ping);
        }
        #endregion

        #region text messages
        /// <summary>
        /// Received a text message from the server.
        /// </summary>
        /// <param name="textMessage"></param>
        public virtual void TextMessage(TextMessage textMessage)
        {
            User user;
            if (!textMessage.ShouldSerializeActor() || !UserDictionary.TryGetValue(textMessage.Actor, out user))   //If we don't know the user for this packet, just ignore it
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

        /// <summary>
        /// Used to send and broadcast text messages.
        /// </summary>
        /// <param name="textMessage"></param>
        public void SendTextMessage(TextMessage textMessage)
        {
            Connection.SendControl(PacketType.TextMessage, textMessage);
        }
        #endregion

        /// <summary>
        /// Lists the registered users.
        /// </summary>
        /// <param name="userList"></param>
        public virtual void UserList(UserList userList)
        {
        }

        public virtual X509Certificate SelectCertificate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return null;
        }

        /// <summary>
        /// Sent by the server to inform the client to refresh its registered user information.
        /// </summary>
        /// <param name="queryUsers"></param>
        public virtual void QueryUsers(QueryUsers queryUsers)
        {
        }

        /// <summary>
        /// The client should fill the IDs or Names of the users it wants to refresh.
        /// The server fills the missing parts and sends the message back.
        /// </summary>
        /// <param name="queryUsers"></param>
        public void SendQueryUsers(QueryUsers queryUsers)
        {
            Connection.SendControl(PacketType.QueryUsers, queryUsers);
        }

        /// <summary>
        /// Sent by the client when it wants to register or clear whisper targets.
        /// Note: The first available target ID is 1 as 0 is reserved for normal talking. Maximum target ID is 30.
        /// </summary>
        /// <param name="voiceTarget"></param>
        public void SendVoiceTarget(VoiceTarget voiceTarget)
        {
            Connection.SendControl(PacketType.VoiceTarget, voiceTarget);
        }

        /// <summary>
        /// Used to communicate user stats between the server and clients.
        /// </summary>
        /// <param name="userStats"></param>
        public virtual void UserStats(UserStats userStats)
        {
        }

        /// <summary>
        /// Used to communicate user stats between the server and clients.
        /// </summary>
        /// <param name="userStats"></param>
        public void SendRequestUserStats(UserStats userStats)
        {
            Connection.SendControl(PacketType.UserStats, userStats);
        }

        /// <summary>
        /// Used by the client to request binary data from the server.
        /// By default large comments or textures are not sent within standard messages but instead the
        /// hash is.
        /// If the client does not recognize the hash it may request the resource when it needs it.
        /// The client does so by sending a RequestBlob message with the correct fields filled with the user sessions or channel_ids it wants to receive.
        /// The server replies to this by sending a new UserState/ChannelState message with the resources filled even if they would normally be transmitted as hashes.
        /// </summary>
        /// <param name="requestBlob"></param>
        public void SendRequestBlob(RequestBlob requestBlob)
        {
            Connection.SendControl(PacketType.RequestBlob, requestBlob);
        }

        /// <summary>
        /// Relays information on the bans.
        /// The server sends this list only after a client queries for it.
        /// </summary>
        /// <param name="banList"></param>
        public virtual void BanList(BanList banList)
        {
        }

        /// <summary>
        /// Relays information on the bans.
        /// The client may send the BanList message to either modify the list of bans or query them from the server.
        /// </summary>
        /// <param name="banList"></param>
        public void SendBanList(BanList banList)
        {
            Connection.SendControl(PacketType.BanList, banList);
        }
    }
}
