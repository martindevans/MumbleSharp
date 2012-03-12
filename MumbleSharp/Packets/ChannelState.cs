using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class ChannelState
    {
        [ProtoMember(1)]
        public UInt32 ChannelId;

        [ProtoMember(2)]
        public UInt32 Parent;

        [ProtoMember(3)]
        public String Name;

        [ProtoMember(4)]
        public UInt32[] Links;

        [ProtoMember(5)]
        public String Description;

        [ProtoMember(6)]
        public UInt32[] LinksAdd;

        [ProtoMember(7)]
        public UInt32[] LinksRemove;

        [ProtoMember(8, IsRequired = false)]
        public bool Temporary;

        [ProtoMember(9, IsRequired = false)]
        public Int32 Position;
    }
}
