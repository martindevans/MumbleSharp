using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MumbleProto;
using MumbleSharp;
using MumbleSharp.Audio;
using MumbleSharp.Audio.Codecs;
using MumbleSharp.Model;
using MumbleSharp.Packets;
using NAudio.Wave;

namespace MumbleGuiClient
{
    public class ConnectionMumbleProtocol : BasicMumbleProtocol
    {
        public delegate void EncodedVoiceDelegate(BasicMumbleProtocol proto, byte[] data, uint userId, long sequence, IVoiceCodec codec, SpeechTarget target);
        public delegate void UserJoinedDelegate(BasicMumbleProtocol proto, User user);
        public delegate void UserStateChangedDelegate(BasicMumbleProtocol proto, User user);
        public delegate void UserStateChannelChangedDelegate(BasicMumbleProtocol proto, User user, uint oldChannelId);
        public delegate void UserLeftDelegate(BasicMumbleProtocol proto, User user);
        public delegate void ChannelJoinedDelegate(BasicMumbleProtocol proto, Channel channel);
        public delegate void ChannelLeftDelegate(BasicMumbleProtocol proto, Channel channel);
        public delegate void ServerConfigDelegate(BasicMumbleProtocol proto, ServerConfig serverConfig);
        public delegate void ChannelMessageReceivedDelegate(BasicMumbleProtocol proto, ChannelMessage message);
        public delegate void PersonalMessageReceivedDelegate(BasicMumbleProtocol proto, PersonalMessage message);

        public EncodedVoiceDelegate encodedVoice;
        public UserJoinedDelegate userJoinedDelegate;
        public UserStateChangedDelegate userStateChangedDelegate;
        public UserStateChannelChangedDelegate userStateChannelChangedDelegate;
        public UserLeftDelegate userLeftDelegate;
        public ChannelJoinedDelegate channelJoinedDelegate;
        public ChannelLeftDelegate channelLeftDelegate;
        public ServerConfigDelegate serverConfigDelegate;
        public ChannelMessageReceivedDelegate channelMessageReceivedDelegate;
        public PersonalMessageReceivedDelegate personalMessageReceivedDelegate;

        public override void EncodedVoice(byte[] data, uint userId, long sequence, IVoiceCodec codec, SpeechTarget target)
        {
            if (encodedVoice != null) encodedVoice(this, data, userId, sequence, codec, target);
            //User user = Users.FirstOrDefault(u => u.Id == userId);
            //if (user != null)
            //    Console.WriteLine(user.Name + " is speaking. Seq" + sequence);

            base.EncodedVoice(data, userId, sequence, codec, target);
        }

        protected override void UserJoined(User user)
        {
            base.UserJoined(user);

            if (userJoinedDelegate != null) userJoinedDelegate(this, user);
        }

        protected override void UserStateChanged(User user)
        {
            base.UserStateChanged(user);

            if (userStateChangedDelegate != null) userStateChangedDelegate(this, user);
        }

        protected override void UserStateChannelChanged(User user, uint oldChannelId)
        {
            base.UserStateChannelChanged(user, oldChannelId);

            if (userStateChannelChangedDelegate != null) userStateChannelChangedDelegate(this, user, oldChannelId);
        }

        protected override void UserLeft(User user)
        {
            base.UserLeft(user);

            if (userLeftDelegate != null) userLeftDelegate(this, user);
        }

        protected override void ChannelJoined(Channel channel)
        {
            base.ChannelJoined(channel);

            if (channelJoinedDelegate != null) channelJoinedDelegate(this, channel);
        }

        protected override void ChannelLeft(Channel channel)
        {
            base.ChannelLeft(channel);

            if (channelLeftDelegate != null) channelLeftDelegate(this, channel);
        }

        public override void ServerConfig(ServerConfig serverConfig)
        {
            base.ServerConfig(serverConfig);

            if (serverConfigDelegate != null) serverConfigDelegate(this, serverConfig);
        }

        protected override void ChannelMessageReceived(ChannelMessage message)
        {
            if (channelMessageReceivedDelegate != null) channelMessageReceivedDelegate(this, message);

            base.ChannelMessageReceived(message);
        }

        protected override void PersonalMessageReceived(PersonalMessage message)
        {
            if (personalMessageReceivedDelegate != null) personalMessageReceivedDelegate(this, message);

            base.PersonalMessageReceived(message);
        }

        public new void SendVoice(ArraySegment<byte> pcm, SpeechTarget target, uint targetId)
        {
            base.SendVoice(pcm,target,targetId);
        }

        public new void SendVoiceStop()
        {
            base.SendVoiceStop();
        }
    }
}
