using MumbleProto;
using MumbleSharp.Audio;
using MumbleSharp.Audio.Codecs;
using MumbleSharp.Packets;
using System;
using NAudio.Wave;

namespace MumbleSharp.Model
{
    public class User
        : IEquatable<User>
    {
        private readonly IMumbleProtocol _owner;

        public UInt32 Id { get; private set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public bool Deaf { get; set; }
        public bool Muted { get; set; }
        public bool SelfDeaf { get; set; }
        public bool SelfMuted { get; set; }
        public bool Suppress { get; set; }

        private Channel _channel;
        public Channel Channel
        {
            get { return _channel; }
            set
            {
                if (_channel != null)
                    _channel.RemoveUser(this);

                _channel = value;

                if (value != null)
                    value.AddUser(this);
            }
        }

        private readonly CodecSet _codecs;

        /// <summary>Initializes a new instance of the <see cref="User"/> class.</summary>
        /// <param name="owner">The mumble protocol used by the User to communicate.</param>
        /// <param name="id">The id of the user.</param>
        /// <param name="audioSampleRate">The sample rate in Hertz (samples per second).</param>
        /// <param name="audioSampleBits">The sample bit depth.</param>
        /// <param name="audioSampleChannels">The sample channels (1 for mono, 2 for stereo).</param>
        /// <param name="audioFrameSize">Size of the frame in samples.</param>
        public User(IMumbleProtocol owner, uint id, int audioSampleRate = Constants.DEFAULT_AUDIO_SAMPLE_RATE, byte audioSampleBits = Constants.DEFAULT_AUDIO_SAMPLE_BITS, byte audioSampleChannels = Constants.DEFAULT_AUDIO_SAMPLE_CHANNELS, ushort audioFrameSize = Constants.DEFAULT_AUDIO_FRAME_SIZE)
        {
            if (owner.Connection.VoiceSupportEnabled)
            {
                _codecs = new CodecSet(audioSampleRate, audioSampleBits, audioSampleChannels, audioFrameSize);
                _buffer = new AudioDecodingBuffer(audioSampleRate, audioSampleBits, audioSampleChannels, audioFrameSize);
            }
            _owner = owner;
            Id = id;
        }

        private static readonly string[] _split = {"\r\n", "\n"};

        /// <summary>
        /// Send a text message
        /// </summary>
        /// <param name="message">A text message (which will be split on newline characters)</param>
        public void SendMessage(string message)
        {
            var messages = message.Split(_split, StringSplitOptions.None);
            SendMessage(messages);
        }

        /// <summary>
        /// Send a text message
        /// </summary>
        /// <param name="message">Individual lines of a text message</param>
        public void SendMessage(string[] message)
        {
            _owner.Connection.SendControl<TextMessage>(PacketType.TextMessage, new TextMessage
            {
                Actor = _owner.LocalUser.Id,
                Message = string.Join(Environment.NewLine, message),
            });
        }

        /// <summary>
        /// Move user to a channel
        /// </summary>
        /// <param name="channel">Channel to move to</param>
        public void Move(Channel channel)
        {
            if (_channel == channel)
                return;

            UserState userstate = new UserState()
            {
                Actor = _owner.LocalUser.Id,
                ChannelId = channel.Id
            };

            if (this.Id != _owner.LocalUser.Id)
            {
                userstate.UserId = this.Id;
            }

            _owner.Connection.SendControl<UserState>(PacketType.UserState, userstate);
        }

        /// <summary>
        /// Send user mute and deaf states
        /// </summary>
        public void SendMuteDeaf()
        {
            UserState userstate = new UserState()
            {
                Actor = _owner.LocalUser.Id
            };

            if(this.Id == _owner.LocalUser.Id)
            {
                userstate.SelfMute = this.SelfMuted || this.SelfDeaf; //mumble disallows being deaf without being muted
                userstate.SelfDeaf = this.SelfDeaf;
            } else
            {
                userstate.UserId = this.Id;
                userstate.Mute = this.Muted || this.Deaf; //mumble disallows being deaf without being muted
                userstate.Deaf = this.Deaf;
            }

            _owner.Connection.SendControl<UserState>(PacketType.UserState, userstate);
        }

        protected internal IVoiceCodec GetCodec(SpeechCodecs codec)
        {
            if (!_owner.Connection.VoiceSupportEnabled)
                throw new InvalidOperationException("Voice Support is disabled with this connection");

            return _codecs.GetCodec(codec);
        }

        public override string ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var u = obj as User;
            if (u != null)
                return (Equals(u));

            return ReferenceEquals(this, obj);
        }

        public bool Equals(User other)
        {
            return other.Id == Id;
        }

        private readonly AudioDecodingBuffer _buffer;
        public IWaveProvider Voice
        {
            get
            {
                if (!_owner.Connection.VoiceSupportEnabled)
                    throw new InvalidOperationException("Voice Support is disabled with this connection");

                return _buffer;
            }
        }

        public void ReceiveEncodedVoice(byte[] data, long sequence, IVoiceCodec codec)
        {
            if (!_owner.Connection.VoiceSupportEnabled)
                throw new InvalidOperationException("Voice Support is disabled with this connection");

            _buffer.AddEncodedPacket(sequence, data, codec);
        }
    }
}
