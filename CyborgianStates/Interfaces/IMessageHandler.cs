using CyborgianStates.MessageHandling;
using System;
using System.Threading.Tasks;

namespace CyborgianStates.Interfaces
{
    public interface IMessageHandler
    {
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        bool IsRunning { get; }

        Task InitAsync();

        Task RunAsync();

        Task ShutdownAsync();
    }

    public class MessageReceivedEventArgs
    {
        public MessageReceivedEventArgs(Message message) => Message = message;

        public Message Message { get; }
    }
}