using System.Threading.Tasks;
using CyborgianStates.CommandHandling;
using CyborgianStates.MessageHandling;

namespace CyborgianStates.Interfaces
{
    public interface IMessageChannel
    {
        Task WriteToAsync(string content);

        Task WriteToAsync(CommandResponse response);

        Task ReplyToAsync(Message message, string content);

        Task ReplyToAsync(Message message, CommandResponse response);

        Task ReplyToAsync(Message message, string content, bool isPublic);

        Task ReplyToAsync(Message message, CommandResponse response, bool isPublic);
    }
}