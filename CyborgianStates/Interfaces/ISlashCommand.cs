using Discord;
using Discord.WebSocket;

namespace CyborgianStates.Interfaces
{
    public interface ISlashCommand : ISlashCommandInteraction
    {
        public bool HasResponded { get; }
        public string CommandName { get; }
        public ISocketMessageChannel Channel { get; }
    }
}