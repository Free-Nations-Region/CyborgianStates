using Dapper.Contrib.Extensions;

namespace CyborgianStates.Data.Models
{
    [Table("StatusEntry")]
    public class StatusEntry
    {
        [Key]
        public ulong Id { get; set; }
        public string Nation { get; set; }
        public string Status { get; set; }
        public string Additional { get; set; }
        public long CreatedAt { get; set; }
        public long? DisabledAt { get; set; }
    }
}
