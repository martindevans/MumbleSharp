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
    /// An object which handles the higher level logic of a connection to a mumble server
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

        bool ReceivedServerSync { get; }

        SpeechCodecs TransmissionCodec { get; }

        void Initialise(MumbleConnection connection);

        bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors);

        X509Certificate SelectCertificate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers);

        void Version(Version version);

        void ChannelState(ChannelState channelState);

        void UserState(UserState userState);

        void CodecVersion(CodecVersion codecVersion);

        void ContextAction(ContextAction contextAction);

        void ContextActionModify(ContextActionModify contextActionModify);

        void PermissionQuery(PermissionQuery permissionQuery);

        void ServerSync(ServerSync serverSync);

        void ServerConfig(ServerConfig serverConfig);

        void EncodedVoice(byte[] packet, uint userSession, long sequence, IVoiceCodec codec, SpeechTarget target);

        void UdpPing(byte[] packet);

        void Ping(Ping ping);

        void UserRemove(UserRemove userRemove);

        void ChannelRemove(ChannelRemove channelRemove);

        void TextMessage(TextMessage textMessage);

        void UserList(UserList userList);

        void SuggestConfig(SuggestConfig suggestedConfiguration);

        IVoiceCodec GetCodec(uint user, SpeechCodecs codec);

        void SendVoice(ArraySegment<byte> pcm, SpeechTarget target, uint targetId);

        void SendVoiceStop();
    }
}
