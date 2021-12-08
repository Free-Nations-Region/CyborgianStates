using Dapper.Contrib.Extensions;

namespace CyborgianStates.Data.Models
{
    [Table("RolePermission")]
    public class RolePermission
    {
        public long PermissionId { get; set; }
        public long RoleId { get; set; }
    }
}