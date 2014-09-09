using System;
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

        [ProtoMember(4)]
        public bool Opus;
    }
}
