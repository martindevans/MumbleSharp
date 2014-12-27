
namespace MumbleSharp.Model
{
    public abstract class Message
    {
        public User Sender { get; protected set; }
        public string Text { get; protected set; }

        protected Message(User sender, string text)
        {
            Sender = sender;
            Text = text;
        }
    }

    public class PersonalMessage : Message
    {
        public PersonalMessage(User sender, string text)
            :base(sender, text)
        {
        }
    }

    public class ChannelMessage : Message
    {
        public Channel Channel { get; protected set; }
        public bool IsRecursive { get; protected set; }

        public ChannelMessage(User sender, string text, Channel channel, bool isRecursive = false)
            :base(sender, text)
        {
            Channel = channel;
            IsRecursive = isRecursive;
        }
    }
}