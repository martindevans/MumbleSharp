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
        private readonly UserState _userstate = new UserState();

        public UInt32 Id { get => _userstate.UserId; private set { _userstate.UserId = value; } }
        public UInt32 ChannelId { get => _userstate.ChannelId; private set { _userstate.ChannelId = value; } }
        public string Name { get => _userstate.Name; set { _userstate.Name = value; } }
        public string Comment { get => _userstate.Comment; set { _userstate.Comment = value; } }
        public bool Deaf { get => _userstate.Deaf; set { _userstate.Deaf = value; } }
        public bool Muted { get => _userstate.Mute; set { _userstate.Mute = value; } }
        public bool SelfDeaf { get => _userstate.SelfDeaf; set { _userstate.SelfDeaf = value; } }
        public bool SelfMuted { get => _userstate.SelfMute; set { _userstate.SelfMute = value; } }
        public bool Suppress { get => _userstate.Suppress; set { _userstate.Suppress = value; } }

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

        private readonly CodecSet _codecs = new CodecSet();

        public User(IMumbleProtocol owner, uint id)
        {
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

            this.ChannelId = channel.Id;

            SendUserState();
        }

        /// <summary>
        /// Send user state
        /// </summary>
        public void SendUserState()
        {
            _owner.Connection.SendControl<UserState>(PacketType.UserState, new UserState()
            {
                Actor = this.Id,
                ChannelId = this.ChannelId,
                SelfMute = this.SelfMuted,
                SelfDeaf = this.SelfDeaf,
            });
        }

        protected internal IVoiceCodec GetCodec(SpeechCodecs codec)
        {
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

        private readonly AudioDecodingBuffer _buffer = new AudioDecodingBuffer();
        public IWaveProvider Voice
        {
            get
            {
                return _buffer;
            }
        }

        public void ReceiveEncodedVoice(byte[] data, long sequence, IVoiceCodec codec)
        {
            _buffer.AddEncodedPacket(sequence, data, codec);
        }
    }
}
