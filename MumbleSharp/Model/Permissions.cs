using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MumbleSharp.Model
{
    public class Permissions
    {
        public const Permission DEFAULT_PERMISSIONS = Permission.Traverse | Permission.Enter | Permission.Speak | Permission.Whisper | Permission.TextMessage;
    }

    [Flags]
    public enum Permission : uint
    {
        //https://github.com/mumble-voip/mumble/blob/master/src/ACL.h
        //https://github.com/mumble-voip/mumble/blob/master/src/ACL.cpp

        /// <summary>
        /// This represents no privileges.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// This represents total access to the channel, including the ability to change group and ACL information.
        /// This privilege implies all other privileges.
        /// </summary>
        Write = 0x1,

        /// <summary>
        /// This represents the permission to traverse the channel.
        /// If a user is denied this privilege, he will be unable to access this channel and any sub-channels in any way, regardless of other permissions in the sub-channels.
        /// </summary>
        Traverse = 0x2,

        /// <summary>
        /// This represents the permission to join the channel.
        /// If you have a hierarchical channel structure, you might want to give everyone Traverse, but restrict Enter in the root of your hierarchy.
        /// </summary>
        Enter = 0x4,

        /// <summary>
        /// This represents the permission to speak in a channel.
        /// Users without this privilege will be suppressed by  the server (seen as muted), and will be unable to speak until they are unmuted by someone with the appropriate privileges.
        /// </summary>
        Speak = 0x8,

        /// <summary>
        /// This represents the permission to mute and deafen other users.
        /// Once muted, a user will stay muted until he is unmuted by another privileged user or reconnects to the server.
        /// </summary>
        MuteDeafen = 0x10,

        /// <summary>
        /// This represents the permission to move a user to another channel or kick him from the server.
        /// To actually move the user, either the moving user must have Move privileges in the destination channel, or the user must normally be allowed to enter the channel.
        /// Users with this privilege can move users into channels the target user normally wouldn't have permission to enter.
        /// </summary>
        Move = 0x20,

        /// <summary>
        /// This represents the permission to make sub-channels.
        /// The user making the sub-channel will be added to the admin group of the sub-channel.
        /// </summary>
        MakeChannel = 0x40,

        /// <summary>
        /// This represents the permission to link channels.
        /// Users in linked channels hear each other, as long as the speaking user has the <i>speak</i> privilege in the channel of the listener.
        /// You need the link privilege in both channels to create a link, but just in either channel to remove it.
        /// </summary>
        LinkChannel = 0x80,

        /// <summary>
        /// This represents the permission to whisper to this channel from the outside.
        /// This works exactly like the <i>speak</i> privilege, but applies to packets spoken with the Whisper key held down.
        /// This may be used to broadcast to a hierarchy of channels without linking.
        /// </summary>
        Whisper = 0x100,

        /// <summary>
        /// This represents the permission to write text messages to other users in this channel.
        /// </summary>
        TextMessage = 0x200,

        /// <summary>
        /// This represents the permission to make a temporary subchannel.
        /// The user making the sub-channel will be added to the admin group of the sub-channel.
        /// Temporary channels are not stored and disappear when the last user leaves.
        /// </summary>
        MakeTempChannel = 0x400,

        // --- Root channel only ---

        /// <summary>
        /// This represents the permission to forcibly remove users from the server.
        /// Root channel only.
        /// </summary>
        Kick = 0x10000,

        /// <summary>
        /// This represents the permission to permanently remove users from the server.
        /// Root channel only.
        /// </summary>
        Ban = 0x20000,

        /// <summary>
        /// This represents the permission to register and unregister users on the server.
        /// Root channel only.
        /// </summary>
        Register = 0x40000,

        /// <summary>
        /// This represents the permission to register oneself on the server.
        /// Root channel only.
        /// </summary>
        SelfRegister = 0x80000,

        Cached = 0x8000000,
        All = 0xf07ff
    };
}
