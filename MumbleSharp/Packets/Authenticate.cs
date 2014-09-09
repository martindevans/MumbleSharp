using System;
using ProtoBuf;

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

        [ProtoMember(5)]
        public bool Opus;
    }
}
