using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MumbleSharp.Model
{
    public class Channel
    {
        public bool Temporary { get; set; }
        public string Name { get; set; }
        public uint Id { get; set; }
        public uint Parent { get; set; }

        public Channel(uint id, string name, uint parent)
        {
            Id = id;
            Name = name;
            Parent = parent;
        }

        public override string ToString()
        {
            return Name;
        }

        internal void RemoveUser(User user)
        {
        }

        internal void AddUser(User user)
        {
        }
    }
}
