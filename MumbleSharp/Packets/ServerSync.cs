using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class ServerSync
    {
        [ProtoMember(1, IsRequired = false)]
        public UInt32 Session;

        [ProtoMember(2, IsRequired = false)]
        public UInt32 MaxBandwidth;

        [ProtoMember(3, IsRequired = false)]
        public String WelcomeText;

        [ProtoMember(4, IsRequired = false)]
        public UInt64 Permissions;
    }
}
