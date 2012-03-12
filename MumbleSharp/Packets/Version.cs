using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using MumbleSharp.Packets;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class Version
    {
        [ProtoMember(1, IsRequired = false)]
        public UInt32 ReleaseVersion;

        [ProtoMember(2, IsRequired = false)]
        public string Release;

        [ProtoMember(3, IsRequired = false)]
        public string os;

        [ProtoMember(4, IsRequired = false)]
        public string os_version;

        public override string ToString()
        {
            return "version:" + ReleaseVersion + " release:" + Release + " os:" + os + " os_version:" + os_version;
        }
    }
}
