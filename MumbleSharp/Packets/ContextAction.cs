using System;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class ContextAction
    {
        [ProtoMember(1, IsRequired = false)]
        public UInt32 Session;

        [ProtoMember(2, IsRequired = false)]
        public UInt32 ChannelId;

        [ProtoMember(2)]
        public String Action;
    }
}
