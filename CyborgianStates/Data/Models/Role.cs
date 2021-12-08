using Dapper.Contrib.Extensions;

namespace CyborgianStates.Data.Models
{
    [Table("Role")]
    public class Role
    {
        [Key]
        public long Id { get; set; }

        public string Name { get; set; }
    }
}