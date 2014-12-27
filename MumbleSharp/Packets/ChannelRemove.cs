using System;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class ChannelRemove
    {
// ReSharper disable UnassignedField.Global
        [ProtoMember(1)]
        public UInt32 ChannelId;
// ReSharper restore UnassignedField.Global
    }
}
