using System.Collections.Generic;
using System.Linq;
using MumbleSharp.Model;
using MumbleSharp.Packets;

namespace MumbleSharp.Extensions
{
    public static class IEnumerableOfChannelExtensions
    {
        public static void SendMessage(this IEnumerable<Channel> channels, string[] message, bool recursive)
        {
            // It's conceivable that this group could include channels from multiple different server connections
            // group by server
            foreach (var group in channels.GroupBy(a => a.Owner))
            {
                var owner = group.First().Owner;

                var msg = new TextMessage
                {
                    Actor = owner.LocalUser.Id,
                    Message = message,
                };

                if (recursive)
                    msg.TreeId = group.Select(c => c.Id).ToArray();
                else
                    msg.ChannelId = group.Select(c => c.Id).ToArray();

                owner.Connection.SendControl<TextMessage>(PacketType.TextMessage, msg);
            }
        }
    }
}
