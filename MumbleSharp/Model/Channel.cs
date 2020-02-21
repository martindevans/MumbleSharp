
using MumbleProto;
using MumbleSharp.Audio;
using MumbleSharp.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MumbleSharp.Model
{
    public class Channel
        : IEquatable<Channel>
    {
        internal readonly IMumbleProtocol Owner;

        public bool Temporary { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Position { get; set; }
        public uint Id { get; private set; }
        public uint Parent { get; internal set; }
        public Permission Permissions { get; internal set; }

        // Using a concurrent dictionary as a concurrent hashset (why doesn't .net provide a concurrent hashset?!) - http://stackoverflow.com/a/18923091/108234
        private readonly ConcurrentDictionary<User, bool> _users = new ConcurrentDictionary<User, bool>();
        public IEnumerable<User> Users
        {
            get { return _users.Keys; }
        }

        public Channel(IMumbleProtocol owner, uint id, string name, uint parent)
        {
            Owner = owner;
            Id = id;
            Name = name;
            Parent = parent;
        }

        /// <summary>
        /// Send a text message
        /// </summary>
        /// <param name="message">Individual lines of a text message</param>
        public void SendMessage(string[] message, bool recursive)
        {
            var msg = new TextMessage
            {
                Actor = Owner.LocalUser.Id,
                Message = string.Join(Environment.NewLine, message),
            };

            if (recursive)
            {
                if (msg.TreeIds == null)
                    msg.TreeIds = new uint[] { Id };
                else
                    msg.TreeIds = msg.TreeIds.Concat(new uint[] { Id }).ToArray();
            }
            else
            {
                if (msg.ChannelIds == null)
                    msg.ChannelIds = new uint[] { Id };
                else
                    msg.ChannelIds = msg.ChannelIds.Concat(new uint[] { Id }).ToArray();
            }

            Owner.Connection.SendControl<TextMessage>(PacketType.TextMessage, msg);
        }

        private static readonly string[] _split = { "\r\n", "\n" };

        /// <summary>
        /// Send a text message
        /// </summary>
        /// <param name="message">A text message (which will be split on newline characters)</param>
        public void SendMessage(string message, bool recursive)
        {
            var messages = message.Split(_split, StringSplitOptions.None);
            SendMessage(messages, recursive);
        }

        public void SendVoice(ArraySegment<byte> buffer, SpeechTarget target = SpeechTarget.Normal)
        {
            Owner.SendVoice(
                buffer,
                target: target,
                targetId: Id
            );
        }

        public void SendVoiceStop()
        {
            Owner.SendVoiceStop();
        }

        public override string ToString()
        {
            return Name;
        }

        public void Join()
        {
            var state = new UserState
            {
                Session = Owner.LocalUser.Id,
                Actor = Owner.LocalUser.Id,
                ChannelId = Id
            };

            Owner.Connection.SendControl<UserState>(PacketType.UserState, state);
        }

        internal void AddUser(User user)
        {
            _users.GetOrAdd(user, true);
        }

        internal void RemoveUser(User user)
        {
            bool _;
            _users.TryRemove(user, out _);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var c = obj as Channel;
            if (c != null)
                return Equals(c);

            return ReferenceEquals(this, obj);
        }

        public bool Equals(Channel other)
        {
            return other.Id == Id;
        }
    }
}
