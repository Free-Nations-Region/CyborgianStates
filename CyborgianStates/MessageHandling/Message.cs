using CyborgianStates.Interfaces;

namespace CyborgianStates.MessageHandling
{
    public class Message
    {
        public Message(ulong authorId, string content, IMessageChannel channel, object msgObj)
        {
            AuthorId = authorId;
            Content = content;
            Channel = channel;
            MessageObject = msgObj;
        }

        public Message(ulong authorId, string content, IMessageChannel channel) : this(authorId, content, channel, null)
        {
        }

        public object MessageObject { get; }
        public ulong AuthorId { get; }
        public IMessageChannel Channel { get; }
        public string Content { get; }
    }
}