using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class ChannelRemove
    {
        [ProtoMember(1)]
        public UInt32 ChannelId;
    }
}
