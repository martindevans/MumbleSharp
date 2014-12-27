using System;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class UserRemove
    {
// ReSharper disable UnassignedField.Global
        [ProtoMember(1)]
        public UInt32 Session;

        [ProtoMember(2, IsRequired = false)]
        public UInt32 Actor;

        [ProtoMember(3, IsRequired = false)]
        public String Reason;
        
        [ProtoMember(4, IsRequired = false)]
        public bool Ban;
// ReSharper restore UnassignedField.Global
    }
}
