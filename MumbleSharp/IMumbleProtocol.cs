using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MumbleSharp.Model;

namespace MumbleSharp
{
    public interface IMumbleProtocol
    {
        User LocalUser { get; }
        Channel RootChannel { get; }

        void Initialise(MumbleConnection connection);

        void Version(MumbleSharp.Packets.Version version);

        void CryptSetup(MumbleSharp.Packets.CryptSetup cryptSetup);

        void ChannelState(MumbleSharp.Packets.ChannelState channelState);

        void UserState(MumbleSharp.Packets.UserState userState);

        void CodecVersion(MumbleSharp.Packets.CodecVersion codecVersion);

        void ContextAction(MumbleSharp.Packets.ContextAction contextAction);

        void ContextActionAdd(Packets.ContextActionAdd contextActionAdd);

        void PermissionQuery(Packets.PermissionQuery permissionQuery);

        void ServerSync(Packets.ServerSync serverSync);

        void ServerConfig(Packets.ServerConfig serverConfig);

        void UdpTunnel(Packets.UdpTunnel tunnel);

        void Udp(byte[] packet);

        void Ping(Packets.Ping ping);

        void UserRemove(Packets.UserRemove userRemove);

        void ChannelRemove(Packets.ChannelRemove channelRemove);

        void TextMessage(Packets.TextMessage textMessage);
    }
}
