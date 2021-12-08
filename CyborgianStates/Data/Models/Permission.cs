using Dapper.Contrib.Extensions;

namespace CyborgianStates.Data.Models
{
    [Table("Permission")]
    public class Permission
    {
        [Key]
        public long Id { get; set; }

        public string Name { get; set; }
    }
}