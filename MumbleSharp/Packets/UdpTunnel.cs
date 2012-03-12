using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class UdpTunnel
    {
        [ProtoMember(1)]
        public byte[] Packet;
    }
}
