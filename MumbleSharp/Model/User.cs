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

        private readonly CodecSet _codecs = new CodecSet();

        public User(IMumbleProtocol owner, uint id)
        {
            _owner = owner;
            Id = id;
        }

        public void SendMessage(string[] message)
        {
            _owner.Connection.SendControl<TextMessage>(PacketType.TextMessage, new TextMessage
            {
                Actor = _owner.LocalUser.Id,
                Message = message,
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
