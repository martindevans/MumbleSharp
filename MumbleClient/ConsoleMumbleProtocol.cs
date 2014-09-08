using System;
using System.Linq;
using MumbleSharp;
using MumbleSharp.Model;

namespace MumbleClient
{
    /// <summary>
    /// A test mumble protocol. Currently just prints the name of whoever is speaking, as well as printing messages it receives
    /// </summary>
    public class ConsoleMumbleProtocol
        : BasicMumbleProtocol
    {
        public override void Voice(byte[] pcm, long userId, long sequence)
        {
            User user = Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
                Console.WriteLine(user.Name + " is speaking. Seq" + sequence);
        }

        protected override void ChannelMessageReceived(ChannelMessage message)
        {
            if (message.Channel.Equals(LocalUser.Channel))
                Console.WriteLine(string.Format("{0} (channel message): {1}", message.Sender.Name, message.Text));

            base.ChannelMessageReceived(message);
        }

        protected override void PersonalMessageReceived(PersonalMessage message)
        {
            Console.WriteLine(string.Format("{0} (personal message): {1}", message.Sender.Name, message.Text));

            base.PersonalMessageReceived(message);
        }
    }
}
