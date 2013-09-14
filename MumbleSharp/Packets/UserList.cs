using System;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class UserList
    {
        [ProtoMember(1)]
        public UserListUser[] Users;
    }

    [ProtoContract]
    public class UserListUser
    {
        [ProtoMember(1, IsRequired=true)]
        public UInt32 UserID;

        [ProtoMember(2, IsRequired=false)]
        public string Name;
    }
}
