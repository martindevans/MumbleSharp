using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class ServerConfig
    {
        [ProtoMember(1, IsRequired = false)]
        public UInt32 MaxBandwidth;

        [ProtoMember(2, IsRequired = false)]
        public String WelcomeText;

        [ProtoMember(3, IsRequired = false)]
        public bool AllowHtml;

        [ProtoMember(4, IsRequired = false)]
        public UInt32 MessageLength;

        [ProtoMember(5, IsRequired = false)]
        public UInt32 ImageMessageLength;
    }
}
