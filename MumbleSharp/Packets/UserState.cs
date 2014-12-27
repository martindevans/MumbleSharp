using System;
using ProtoBuf;

namespace MumbleSharp.Packets
{
    [ProtoContract]
    public class UserState
    {
// ReSharper disable UnassignedField.Global
        [ProtoMember(1)]
        public UInt32? Session;

        [ProtoMember(2)]
        public UInt32? Actor;

        [ProtoMember(3)]
        public String Name;

        [ProtoMember(4)]
        public UInt32? UserId;

        [ProtoMember(5)]
        public UInt32? ChannelId;

        [ProtoMember(6)]
        public bool? Mute;

        [ProtoMember(7)]
        public bool? Deaf;

        [ProtoMember(8)]
        public bool? Suppress;

        [ProtoMember(9)]
        public bool? SelfMute;

        [ProtoMember(10)]
        public bool? SelfDeaf;

        [ProtoMember(11)]
        public byte[] Texture;

        [ProtoMember(12)]
        public byte[] PluginContext;

        [ProtoMember(13)]
        public String PluginIdentity;

        [ProtoMember(14)]
        public String Comment;

        [ProtoMember(15)]
        public String Hash;

        [ProtoMember(16)]
        public byte[] CommentHash;

        [ProtoMember(17)]
        public byte[] TextureHash;

        [ProtoMember(18)]
        public bool? PrioritySpeaker;

        [ProtoMember(19)]
        public bool? Recording;
// ReSharper restore UnassignedField.Global
    }
}
