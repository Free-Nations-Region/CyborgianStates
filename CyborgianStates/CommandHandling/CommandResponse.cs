using Discord;

namespace CyborgianStates.CommandHandling
{
    public enum CommandStatus
    {
        Success,
        Error
    }

    public class CommandResponse
    {
        internal CommandResponse() { }
        public CommandResponse(CommandStatus status, string content)
        {
            Status = status;
            Content = content;
        }

        public string Content { get; internal set; }
        public CommandStatus Status { get; internal set; }
        public Embed ResponseObject { get; internal set; }
    }
}