using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class CodecVersion
    {
        [ProtoMember(1)]
        public Int32 Alpha;

        [ProtoMember(2)]
        public Int32 Beta;

        [ProtoMember(3)]
        public bool PreferAlpha;
    }
}
