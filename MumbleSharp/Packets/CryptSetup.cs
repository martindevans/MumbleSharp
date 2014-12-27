using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class CryptSetup
    {
// ReSharper disable UnassignedField.Global
        [ProtoMember(1, IsRequired = false)]
        public byte[] Key;

        [ProtoMember(2, IsRequired = false)]
        public byte[] ClientNonce;

        [ProtoMember(3, IsRequired = false)]
        public byte[] ServerNonce;
// ReSharper restore UnassignedField.Global
    }
}
