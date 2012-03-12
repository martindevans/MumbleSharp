using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MumbleSharp.Model
{
    public class User
        :IDisposable
    {
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

        public User(uint id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return Name;
        }

        public void Dispose()
        {
            Channel = null;
        }
    }
}
