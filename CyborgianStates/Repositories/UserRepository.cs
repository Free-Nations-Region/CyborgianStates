using CyborgianStates.Data.Models;
using CyborgianStates.Interfaces;
using DataAbstractions.Dapper;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CyborgianStates.Repositories
{
    public class UserRepository : IUserRepository
    {
        #region SQLs

        private readonly string _getUserByExternalIdSql;
        private readonly string _isUserInDbSql;
        private readonly string _rolePermissionsSql;
        private readonly string _userPermissionsSql;

        #endregion SQLs

        private readonly IDataAccessor _dataAccessor;
        private readonly ISqlProvider _sql;
        private readonly AppSettings _appSettings;

        public UserRepository(IDataAccessor dbConnection, ISqlProvider sql, IOptions<AppSettings> options)
        {
            if (dbConnection is null)
            {
                throw new ArgumentNullException(nameof(dbConnection));
            }

            if (sql is null)
            {
                throw new ArgumentNullException(nameof(sql));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _dataAccessor = dbConnection;
            _sql = sql;
            _appSettings = options.Value;
            _dataAccessor.ConnectionString = _appSettings.DbConnection;
            _isUserInDbSql = _sql.GetSql("User.IsInDb");
            _getUserByExternalIdSql = _sql.GetSql("User.GetByExternalId");
            _rolePermissionsSql = _sql.GetSql("User.RolePermissions");
            _userPermissionsSql = _sql.GetSql("User.Permissions");
        }

        public async Task AddUserToDbAsync(ulong userId) => await _dataAccessor.InsertAsync(new User() { ExternalUserId = (long) userId }).ConfigureAwait(false);

        public async Task<User> GetUserByExternalUserIdAsync(ulong externalUserId) => await _dataAccessor.QueryFirstOrDefaultAsync<User>(_getUserByExternalIdSql, new { ExternalUserId = externalUserId }).ConfigureAwait(false);

        public async Task<User> GetUserByIdAsync(ulong userId) => await _dataAccessor.GetAsync<User>(userId).ConfigureAwait(false);

        public async Task<bool> IsAllowedAsync(string permissionType, ulong userId)
        {
            if (string.IsNullOrWhiteSpace(permissionType))
            {
                throw new ArgumentNullException(nameof(permissionType), "The permissionType can't be empty.");
            }

            if (_appSettings.ExternalAdminUserId == userId)
            {
                return true;
            }

            IEnumerable<dynamic> res1 = await _dataAccessor.QueryAsync(_rolePermissionsSql, new { ExternalUserId = userId }, null, null, null).ConfigureAwait(false);
            IEnumerable<dynamic> res2 = await _dataAccessor.QueryAsync(_userPermissionsSql, new { ExternalUserId = userId }, null, null, null).ConfigureAwait(false);
            var perms = res1.Select<dynamic, string>(r => r.Name).ToHashSet();
            perms.UnionWith(res2.Select<dynamic, string>(r => r.Name));
            perms = perms.Distinct().ToHashSet();
            if (!perms.Contains(permissionType))
            {
                if (perms.Any(p => p.EndsWith("*", StringComparison.InvariantCulture)))
                {
                    string rootString = permissionType[..permissionType.LastIndexOf(".", StringComparison.InvariantCulture)];
                    var listParts = new List<string>
                    {
                        "*",
                        permissionType[..permissionType.IndexOf(".", StringComparison.InvariantCulture)]
                    };
                    while (rootString.Contains('.', StringComparison.InvariantCulture))
                    {
                        listParts.Add(rootString);
                        rootString = rootString[..rootString.LastIndexOf(".", StringComparison.InvariantCulture)];
                    }
                    var rootPerms = listParts.Select(p => $"{p}.*").ToHashSet();
                    return perms.Overlaps(rootPerms);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        public async Task<bool> IsUserInDbAsync(ulong userId) => await _dataAccessor.QueryFirstOrDefaultAsync(_isUserInDbSql, new { ExternalUserId = userId }, null, null, null).ConfigureAwait(false) != null;

        public async Task RemoveUserFromDbAsync(User user) => await _dataAccessor.DeleteAsync(user).ConfigureAwait(false);
    }
}