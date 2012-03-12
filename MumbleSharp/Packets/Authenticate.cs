using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using MumbleSharp.Packets;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class Authenticate
    {
        [ProtoMember(1, IsRequired = false)]
        public string Username;

        [ProtoMember(2, IsRequired = false)]
        public string Password;

        [ProtoMember(3)]
        public string[] Tokens;

        [ProtoMember(4)]
        public Int32[] CeltVersions;
    }
}
