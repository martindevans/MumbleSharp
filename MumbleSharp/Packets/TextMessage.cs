using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class TextMessage
    {
        [ProtoMember(1, IsRequired = false)]
        public UInt32 Actor;

        [ProtoMember(2)]
        public UInt32[] Session;

        [ProtoMember(3)]
        public UInt32[] ChannelId;

        [ProtoMember(4)]
        public UInt32[] TreeId;

        [ProtoMember(5)]
        public String[] Message;
    }
}
