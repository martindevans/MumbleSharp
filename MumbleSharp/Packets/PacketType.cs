
namespace MumbleSharp.Packets
{
    public enum PacketType
        :short
    {
        Version = 0,

        /// <summary>
        /// Not used. Not even for tunneling UDP through TCP.
        /// </summary>
        UDPTunnel = 1,

        /// <summary>
        /// Used by the client to send the authentication credentials to the server.
        /// </summary>
        Authenticate = 2,

        /// <summary>
        /// Sent by the client to notify the server that the client is still alive.
        /// Server must reply to the packet with the same timestamp and its own good/late/lost/resync numbers. None of the fields is strictly required.
        /// </summary>
        Ping = 3,

        /// <summary>
        /// Sent by the server when it rejects the user connection.
        /// </summary>
        Reject = 4,

        /// <summary>
        /// ServerSync message is sent by the server when it has authenticated the user and finished synchronizing the server state.
        /// </summary>
        ServerSync = 5,

        /// <summary>
        /// Sent by the client when it wants a channel removed. Sent by the server when a channel has been removed and clients should be notified.
        /// </summary>
        ChannelRemove = 6,

        /// <summary>
        /// Used to communicate channel properties between the client and the server.
        /// Sent by the server during the login process or when channel properties are updated.
        /// Client may use this message to update said channel properties.
        /// </summary>
        ChannelState = 7,

        /// <summary>
        /// Used to communicate user leaving or being kicked. May be sent by the client
        /// when it attempts to kick a user. Sent by the server when it informs the clients that a user is not present anymore.
        /// </summary>
        UserRemove = 8,

        /// <summary>
        /// Sent by the server when it communicates new and changed users to client.
        /// First seen during login procedure. May be sent by the client when it wishes to alter its state.
        /// </summary>
        UserState = 9,

        /// <summary>
        /// Relays information on the bans.
        /// The client may send the BanList message to either modify the list of bans or query them from the server.
        /// The server sends this list only after a client queries for it.
        /// </summary>
        BanList = 10,

        /// <summary>
        /// Used to send and broadcast text messages.
        /// </summary>
        TextMessage = 11,


        PermissionDenied = 12,


        ACL = 13,

        /// <summary>
        /// Client may use this message to refresh its registered user information.
        /// The client should fill the IDs or Names of the users it wants to refresh.
        /// The server fills the missing parts and sends the message back.
        /// </summary>
        QueryUsers = 14,

        /// <summary>
        /// Used to initialize and resync the UDP encryption. Either side may request a resync by sending the message without any values filled.
        /// The resync is performed by sending the message with only the client or server nonce filled.
        /// </summary>
        CryptSetup = 15,


        ContextActionModify= 16,

        /// <summary>
        /// Sent by the client when it wants to initiate a Context action.
        /// </summary>
        ContextAction = 17,

        /// <summary>
        /// Lists the registered users.
        /// </summary>
        UserList = 18,

        /// <summary>
        /// Sent by the client when it wants to register or clear whisper targets.
        /// Note: The first available target ID is 1 as 0 is reserved for normal talking. Maximum target ID is 30.
        /// </summary>
        VoiceTarget = 19,

        /// <summary>
        /// Sent by the client when it wants permissions for a certain channel.
        /// Sent by the server when it replies to the query or wants the user to resync all channel permissions.
        /// </summary>
        PermissionQuery = 20,

        /// <summary>
        /// Sent by the server to notify the users of the version of the CELT codec they should use.
        /// This may change during the connection when new users join.
        /// </summary>
        CodecVersion = 21,

        /// <summary>
        /// Used to communicate user stats between the server and clients.
        /// </summary>
        UserStats = 22,

        /// <summary>
        /// Used by the client to request binary data from the server.
        /// By default large comments or textures are not sent within standard messages but instead the hash is.
        /// If the client does not recognize the hash it may request the resource when it needs it.
        /// The client does so by sending a RequestBlob message with the correct fields filled with the user sessions or channel_ids it wants to receive.
        /// The server replies to this by sending a new UserState/ChannelState message with the resources filled even if they would normally be transmitted as hashes.
        /// </summary>
        RequestBlob = 23,

        /// <summary>
        /// Sent by the server when it informs the clients on server configuration details.
        /// </summary>
        ServerConfig = 24,

        /// <summary>
        /// Sent by the server to inform the clients of suggested client configuration specified by the server administrator.
        /// </summary>
        SuggestConfig = 25,


        Empty = 32767
    }
}
