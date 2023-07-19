using CyborgianStates.Interfaces;
using System.Collections.Generic;

namespace CyborgianStates.Data
{
    public class SqliteSqlProvider : ISqlProvider // -> BaseSqlProvider with all SQLs that are the same for all Datbases
    {
        private Dictionary<string, string> sqlDict = new();

        public SqliteSqlProvider() => InitDictionary();

        public string GetSql(string key) => sqlDict.TryGetValue(key, out string sql) ? sql : throw new KeyNotFoundException($"No sql for key '{key}' found.");

        private void InitDictionary()
        {
            sqlDict = new Dictionary<string, string>()
            {
                ["User.IsInDb"] = "SELECT 1 FROM User WHERE ExternalUserId = @ExternalUserId",
                ["User.GetByExternalId"] = "SELECT * FROM User WHERE ExternalUserId = @ExternalUserId",
                ["User.RolePermissions"] = @"SELECT
	                    DISTINCT p.Name
                    FROM
	                    Permission p
	                JOIN RolePermission rp ON rp.PermissionId = p.Id
	                JOIN UserRole ur ON ur.RoleId = rp.RoleId
	                JOIN User u on u.Id = ur.UserId
                    WHERE u.ExternalUserId = @ExternalUserId",
                ["User.Permissions"] = @"SELECT
	                   DISTINCT p.Name
                    FROM
	                   Permission p
	                JOIN UserPermission up ON up.PermissionId = p.Id
	                JOIN User u on u.Id = up.UserId
                    WHERE u.ExternalUserId = @ExternalUserId"
            };
        }
    }
}