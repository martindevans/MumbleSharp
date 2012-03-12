using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class Ping
    {
        [ProtoMember(1, IsRequired = false)]
        public UInt64 TimeStamp;

        [ProtoMember(2, IsRequired = false)]
        public UInt32 Good;

        [ProtoMember(3, IsRequired = false)]
        public UInt32 Late;

        [ProtoMember(4, IsRequired = false)]
        public UInt32 Lost;

        [ProtoMember(5, IsRequired = false)]
        public UInt32 Resync;

        [ProtoMember(6, IsRequired = false)]
        public UInt32 UdpPackets;

        [ProtoMember(7, IsRequired = false)]
        public UInt32 TcpPackets;

        [ProtoMember(8, IsRequired = false)]
        public float UdpPingAvg;

        [ProtoMember(9, IsRequired = false)]
        public float UdpPingVar;

        [ProtoMember(10, IsRequired = false)]
        public float TcpPingAvg;

        [ProtoMember(11, IsRequired = false)]
        public float TcpPingVar;
    }
}
