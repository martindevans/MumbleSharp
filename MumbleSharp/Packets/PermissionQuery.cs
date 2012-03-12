using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class PermissionQuery
    {
        [ProtoMember(1, IsRequired = false)]
        public UInt32 ChannelId;

        [ProtoMember(2, IsRequired = false)]
        public UInt32 Permissions;

        [ProtoMember(3, IsRequired = false)]
        public bool Flush;
    }
}
