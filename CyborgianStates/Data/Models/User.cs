using Dapper.Contrib.Extensions;

namespace CyborgianStates.Data.Models
{
    [Table("User")]
    public class User
    {
        public long ExternalUserId { get; set; }

        [Key]
        public long Id { get; set; }
    }
}