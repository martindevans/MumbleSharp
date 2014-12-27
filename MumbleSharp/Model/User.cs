using MumbleSharp.Audio;
using MumbleSharp.Audio.Codecs;
using MumbleSharp.Audio.Codecs.CeltaAlpha;
using MumbleSharp.Audio.Codecs.CeltBeta;
using MumbleSharp.Audio.Codecs.Opus;
using MumbleSharp.Audio.Codecs.Speex;
using MumbleSharp.Packets;
using System;
using NAudio.Wave;

namespace MumbleSharp.Model
{
    public class User
        : IEquatable<User>
    {
        internal readonly IMumbleProtocol Owner;

        public UInt32 Id { get; private set; }
        public bool Deaf { get; set; }
        public bool Muted { get; set; }

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

        public string Name { get; set; }
        public string Comment { get; set; }

        public User(IMumbleProtocol owner, uint id)
        {
            Owner = owner;
            Id = id;
        }

        public void SendMessage(string[] message)
        {
            Owner.Connection.SendControl<TextMessage>(PacketType.TextMessage, new TextMessage
            {
                Actor = Owner.LocalUser.Id,
                Message = message,
            });
        }

        private readonly Lazy<CeltAlphaCodec> _alpha = new Lazy<CeltAlphaCodec>();
        private readonly Lazy<CeltBetaCodec> _beta = new Lazy<CeltBetaCodec>();
        private readonly Lazy<SpeexCodec> _speex = new Lazy<SpeexCodec>();
        private readonly Lazy<OpusCodec> _opus = new Lazy<OpusCodec>();
        protected internal IVoiceCodec GetCodec(SpeechCodecs codec)
        {
            switch (codec)
            {
                case SpeechCodecs.CeltAlpha:
                    return _alpha.Value;
                case SpeechCodecs.Speex:
                    return _speex.Value;
                case SpeechCodecs.CeltBeta:
                    return _beta.Value;
                case SpeechCodecs.Opus:
                    return _opus.Value;
                default:
                    throw new ArgumentOutOfRangeException("codec");
            }
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

        private readonly AudioBuffer _buffer = new AudioBuffer();
        public IWaveProvider Voice
        {
            get
            {
                return _buffer;
            }
        }

        public void EncodedVoice(byte[] data, long sequence, IVoiceCodec codec)
        {
            _buffer.AddEncodedPacket(sequence, data, codec);
        }
    }
}
