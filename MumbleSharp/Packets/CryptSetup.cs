using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class CryptSetup
    {
        [ProtoMember(1, IsRequired = false)]
        public byte[] Key;

        [ProtoMember(2, IsRequired = false)]
        public byte[] ClientNonce;

        [ProtoMember(3, IsRequired = false)]
        public byte[] ServerNonce;
    }
}
