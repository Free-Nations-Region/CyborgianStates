using Dapper.Contrib.Extensions;

namespace CyborgianStates.Data.Models
{
    [Table("CommandUsage")]
    public class CommandUsage
    {
        [Key]
        public ulong Id { get; set; }
        public string TraceId { get; set; }
        public long Timestamp { get; set; }
        public ulong UserId { get; set; }
        public ulong ChannelId { get; set; }
        public bool IsPrimaryGuild { get; set; }
        public bool IsDM { get; set; }
        public ulong GuildId { get; set; }
        public CommandType CommandType { get; set; }
        public string Command { get; set; }
        public double CompleteTime { get; set; }
    }

    public enum CommandType
    {
        Message,
        SlashCommand,
        MessageComponent,
        ContextCommand
    }
}
