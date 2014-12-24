using ProtoBuf;
using System;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class SuggestConfig
    {
        [ProtoMember(1, IsRequired = false)]
        public UInt32 Version;

        [ProtoMember(2, IsRequired = false)]
        public bool Positional = true;

        [ProtoMember(3, IsRequired = false)]
        public bool PushToTalk = true;
    }
}
