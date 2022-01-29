using Discord;
using Discord.WebSocket;

namespace CyborgianStates.Interfaces
{
    public interface ISlashCommand : ISlashCommandInteraction
    {
        public string CommandName { get; }
        public ISocketMessageChannel Channel { get; }
    }
}