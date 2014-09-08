using System;
using ProtoBuf;

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
