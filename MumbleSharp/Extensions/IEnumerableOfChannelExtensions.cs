using System;
using System.Collections.Generic;
using System.Linq;
using MumbleProto;
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
                    actor = owner.LocalUser.Id,
                    message = string.Join(Environment.NewLine, message),
                };

                if (recursive)
                    msg.tree_id.AddRange(group.Select(c => c.Id));
                else
                    msg.channel_id.AddRange(group.Select(c => c.Id));

                owner.Connection.SendControl<TextMessage>(PacketType.TextMessage, msg);
            }
        }
    }
}
