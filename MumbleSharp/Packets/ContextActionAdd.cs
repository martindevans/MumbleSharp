using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class ContextActionAdd
    {
        [ProtoMember(1)]
        public String Action;

        [ProtoMember(2)]
        public String Text;

        [ProtoMember(3, IsRequired = false)]
        public UInt32 Context;
    }
}
