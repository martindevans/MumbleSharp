using MumbleProto;
using MumbleSharp.Audio;
using MumbleSharp.Audio.Codecs;
using MumbleSharp.Model;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Version = MumbleProto.Version;

namespace MumbleSharp
{
    /// <summary>
    /// An object which handles the higher level logic of a connection to a mumble server to support reception of all mumble packet types.
    /// </summary>
    public interface IMumbleProtocol
    {
        MumbleConnection Connection { get; }

        /// <summary>
        /// The user of the local client
        /// </summary>
        User LocalUser { get; }

        /// <summary>
        /// The root channel of the server
        /// </summary>
        Channel RootChannel { get; }

        /// <summary>
        /// All channels on the server
        /// </summary>
        IEnumerable<Channel> Channels { get; }

        /// <summary>
        /// All users on the server
        /// </summary>
        IEnumerable<User> Users { get; }

        /// <summary>
        /// If true, this indicates that the connection was setup and the server accept this client
        /// </summary>
        bool ReceivedServerSync { get; }

        SpeechCodecs TransmissionCodec { get; }

        /// <summary>
        /// Associates this protocol with an opening mumble connection
        /// </summary>
        /// <param name="connection"></param>
        void Initialise(MumbleConnection connection);

        /// <summary>
        /// Validate the certificate the server sends for itself. By default this will acept *all* certificates.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors);

        X509Certificate SelectCertificate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers);

        /// <summary>
        /// Server has sent a version update.
        /// </summary>
        /// <param name="version"></param>
        void Version(Version version);

        /// <summary>
        /// Used to communicate channel properties between the client and the server.
        /// Sent by the server during the login process or when channel properties are updated.
        /// </summary>
        /// <param name="channelState"></param>
        void ChannelState(ChannelState channelState);

        /// <summary>
        /// Sent by the server when it communicates new and changed users to client.
        /// First seen during login procedure.
        /// </summary>
        /// <param name="userState"></param>
        void UserState(UserState userState);

        // Authenticate is only sent from client to server (see https://github.com/mumble-voip/mumble/blob/master/src/Mumble.proto)
        //void Authenticate(Authenticate authenticate);

        void CodecVersion(CodecVersion codecVersion);

        void ContextAction(ContextAction contextAction);

        // ContextActionModify is only sent from client to server (see https://github.com/mumble-voip/mumble/blob/master/src/Mumble.proto)
        //void ContextActionModify(ContextActionModify contextActionModify);

        /// <summary>
        /// Sent by the server when it replies to the query or wants the user to resync all channel permissions.
        /// </summary>
        /// <param name="permissionQuery"></param>
        void PermissionQuery(PermissionQuery permissionQuery);

        /// <summary>
        /// ServerSync message is sent by the server when it has authenticated the user and finished synchronizing the server state.
        /// </summary>
        /// <param name="serverSync"></param>
        void ServerSync(ServerSync serverSync);

        /// <summary>
        /// Sent by the server when it informs the clients on server configuration details.
        /// </summary>
        /// <param name="serverConfig"></param>
        void ServerConfig(ServerConfig serverConfig);

        /// <summary>
        /// Received a voice packet from the server
        /// </summary>
        /// <param name="data"></param>
        /// <param name="userId"></param>
        /// <param name="sequence"></param>
        /// <param name="codec"></param>
        /// <param name="target"></param>
        void EncodedVoice(byte[] packet, uint userSession, long sequence, IVoiceCodec codec, SpeechTarget target);

        /// <summary>
        /// Received a UDP ping from the server
        /// </summary>
        /// <param name="packet"></param>
        void UdpPing(byte[] packet);

        /// <summary>
        /// Received a ping over the TCP connection.
        /// Server must reply to the client Ping packet with the same timestamp and its own good/late/lost/resync numbers. None of the fields is strictly required.
        /// </summary>
        /// <param name="ping"></param>
        void Ping(Ping ping);

        /// <summary>
        /// Used to communicate user leaving or being kicked.
        /// Sent by the server when it informs the clients that a user is not present anymore.
        /// </summary>
        /// <param name="userRemove"></param>
        void UserRemove(UserRemove userRemove);

        /// <summary>
        /// Sent by the server when a channel has been removed and clients should be notified.
        /// </summary>
        /// <param name="channelRemove"></param>
        void ChannelRemove(ChannelRemove channelRemove);

        /// <summary>
        /// Received a text message from the server.
        /// </summary>
        /// <param name="textMessage"></param>
        void TextMessage(TextMessage textMessage);

        /// <summary>
        /// Lists the registered users.
        /// </summary>
        /// <param name="userList"></param>
        void UserList(UserList userList);

        /// <summary>
        /// Sent by the server to inform the clients of suggested client configuration specified by the server administrator.
        /// </summary>
        /// <param name="config"></param>
        void SuggestConfig(SuggestConfig suggestedConfiguration);

        /// <summary>
        /// Get a voice decoder for the specified user/codec combination
        /// </summary>
        /// <param name="session"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        IVoiceCodec GetCodec(uint user, SpeechCodecs codec);

        void SendVoice(ArraySegment<byte> pcm, SpeechTarget target, uint targetId);

        void SendVoiceStop();

        /// <summary>
        /// Sent by the server when it rejects the user connection.
        /// </summary>
        /// <param name="reject"></param>
        void Reject(Reject reject);

        void PermissionDenied(PermissionDenied permissionDenied);

        void Acl(Acl acl);

        /// <summary>
        /// Sent by the server to inform the client to refresh its registered user information.
        /// </summary>
        /// <param name="queryUsers"></param>
        void QueryUsers(QueryUsers queryUsers);

        // VoiceTarget is only sent from client to server (see https://github.com/mumble-voip/mumble/blob/master/src/Mumble.proto)
        //void VoiceTarget(VoiceTarget voiceTarget);

        /// <summary>
        /// Used to communicate user stats between the server and clients.
        /// </summary>
        void UserStats(UserStats userStats);

        // RequestBlob is only sent from client to server (see https://github.com/mumble-voip/mumble/blob/master/src/Mumble.proto)
        //void RequestBlob(RequestBlob requestBlob);

        /// <summary>
        /// Relays information on the bans.
        /// The server sends this list only after a client queries for it.
        /// </summary>
        void BanList(BanList banList);
    }
}
