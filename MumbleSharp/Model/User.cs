using System;
using MumbleSharp.Packets;

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
    }
}
