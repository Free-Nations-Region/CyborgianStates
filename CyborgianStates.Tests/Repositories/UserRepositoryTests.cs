using CyborgianStates.Data;
using CyborgianStates.Interfaces;
using CyborgianStates.Data.Models;
using CyborgianStates.Repositories;
using DataAbstractions.Dapper;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace CyborgianStates.Tests.Repositories
{
    public class UserRepositoryTests
    {
        private Mock<IOptions<AppSettings>> appSettingsMock;
        private Mock<IDataAccessor> dataAccessorMock;
        private ISqlProvider sqlProvider = new SqliteSqlProvider();
        private UserRepository userRepo;

        public UserRepositoryTests()
        {
            dataAccessorMock = new Mock<IDataAccessor>(MockBehavior.Strict);
            dataAccessorMock.SetupGet<string>(db => db.ConnectionString).Returns(string.Empty);
            dataAccessorMock.SetupSet<string>(db => db.ConnectionString = It.IsAny<string>());
            dataAccessorMock.SetupGet<ConnectionState>(db => db.State).Returns(ConnectionState.Open);
            appSettingsMock = new Mock<IOptions<AppSettings>>(MockBehavior.Strict);
            appSettingsMock
                .Setup(m => m.Value)
                .Returns(new AppSettings() { Contact = "", ExternalAdminUserId = 0 });
            userRepo = new UserRepository(dataAccessorMock.Object, sqlProvider, appSettingsMock.Object);
        }

        [Fact]
        public async Task TestAddUserToDb()
        {
            dataAccessorMock.Setup(db => db.InsertAsync(It.IsAny<User>(), null, null, null)).Returns(Task.FromResult(1));
            await userRepo.AddUserToDbAsync(1).ConfigureAwait(false);
            dataAccessorMock.Verify(db => db.InsertAsync(It.IsAny<User>(), null, null, null), Times.Once);
        }

        [Fact]
        public async Task TestAllowedDirectUserPermission()
        {
            var perm = new ExpandoObject() as dynamic;
            perm.Name = "Commands.Execute";
            var userPermissions = new List<dynamic>() { perm } as IEnumerable<dynamic>;
            var res = await PermissionTest("Commands.Execute", userPermissions, Enumerable.Empty<dynamic>()).ConfigureAwait(false);
            res.Should().BeTrue();
        }

        [Fact]
        public async Task TestAllowedRolePermission()
        {
            var perm = new ExpandoObject() as dynamic;
            perm.Name = "Commands.Execute";
            var rolePermissions = new List<dynamic>() { perm } as IEnumerable<dynamic>;
            var res = await PermissionTest("Commands.Execute", Enumerable.Empty<dynamic>(), rolePermissions).ConfigureAwait(false);
            res.Should().BeTrue();
        }

        [Fact]
        public async Task TestAllowedWildcardRolePermission()
        {
            var perm = new ExpandoObject() as dynamic;
            perm.Name = "Commands.*";
            var rolePermissions = new List<dynamic>() { perm } as IEnumerable<dynamic>;
            var res = await PermissionTest("Commands.Execute", Enumerable.Empty<dynamic>(), rolePermissions).ConfigureAwait(false);
            res.Should().BeTrue();
        }

        [Fact]
        public async Task TestAllowedWildcardUserPermission()
        {
            var perm = new ExpandoObject() as dynamic;
            perm.Name = "Commands.*";
            var userPermissions = new List<dynamic>() { perm } as IEnumerable<dynamic>;
            var res = await PermissionTest("Commands.Execute", userPermissions, Enumerable.Empty<dynamic>()).ConfigureAwait(false);
            res.Should().BeTrue();
            perm.Name = "Commands.Preview.*";
            res = await PermissionTest("Commands.Preview.Execute", userPermissions, Enumerable.Empty<dynamic>()).ConfigureAwait(false);
            res.Should().BeTrue();
        }

        [Fact]
        public async Task TestAllowedWithEmptyPermissionType()
        {
            Assert.Throws<ArgumentNullException>(() => new UserRepository(dataAccessorMock.Object, sqlProvider, null));
            var userRepo = new UserRepository(dataAccessorMock.Object, sqlProvider, appSettingsMock.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await userRepo.IsAllowedAsync(null, 0).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestAlwaysAllowedForBotAdmin()
        {
            var userRepo = new UserRepository(dataAccessorMock.Object, sqlProvider, appSettingsMock.Object);
            Assert.True(await userRepo.IsAllowedAsync("*.*", 0).ConfigureAwait(false));
        }

        [Fact]
        public void TestCreateRepository()
        {
            Assert.Throws<ArgumentNullException>(() => new UserRepository(null, sqlProvider, appSettingsMock.Object));
            Assert.Throws<ArgumentNullException>(() => new UserRepository(dataAccessorMock.Object, null, appSettingsMock.Object));
            Assert.Throws<ArgumentNullException>(() => new UserRepository(dataAccessorMock.Object, sqlProvider, null));
            var userRepo = new UserRepository(dataAccessorMock.Object, sqlProvider, appSettingsMock.Object);
        }

        [Fact]
        public async Task TestDeniedDirectPermission()
        {
            var perm = new ExpandoObject() as dynamic;
            perm.Name = "Commands.Preview";
            var rolePermissions = new List<dynamic>() { perm } as IEnumerable<dynamic>;
            var res = await PermissionTest("Commands.Execute", Enumerable.Empty<dynamic>(), rolePermissions).ConfigureAwait(false);
            res.Should().BeFalse();
        }

        [Fact]
        public async Task TestDeniedWildcardRolePermission()
        {
            var perm = new ExpandoObject() as dynamic;
            perm.Name = "Commands.Preview.*";
            var rolePermissions = new List<dynamic>() { perm } as IEnumerable<dynamic>;
            var res = await PermissionTest("Commands.Execute", Enumerable.Empty<dynamic>(), rolePermissions).ConfigureAwait(false);
            res.Should().BeFalse();
        }

        [Fact]
        public async Task TestGetUserById()
        {
            dataAccessorMock.Setup(db => db.GetAsync<User>(It.IsAny<ulong>(), null, null)).Returns(Task.FromResult(new User()));
            await userRepo.GetUserByIdAsync(1).ConfigureAwait(false);
            dataAccessorMock.Setup(db => db.QueryFirstOrDefaultAsync<User>(It.IsAny<string>(), It.IsAny<object>(), null, null, null)).Returns(Task.FromResult(new User()));
            await userRepo.GetUserByExternalUserIdAsync(1).ConfigureAwait(false);
            dataAccessorMock.Verify(db => db.GetAsync<User>(It.IsAny<ulong>(), null, null), Times.Once);
        }

        [Fact]
        public async Task TestIsUserInDb()
        {
            var userInDbSql = sqlProvider.GetSql("User.IsInDb");
            var user = new ExpandoObject();
            dataAccessorMock
                .Setup(db => db.QueryFirstOrDefaultAsync(userInDbSql, It.IsAny<object>(), null, null, null))
                .Returns(Task.FromResult(user as object));
            var userRepo = new UserRepository(dataAccessorMock.Object, sqlProvider, appSettingsMock.Object);
            await userRepo.IsUserInDbAsync(1).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestRemoveUserFromDb()
        {
            dataAccessorMock.Setup(db => db.DeleteAsync(It.IsAny<User>(), null, null)).Returns(Task.FromResult(true));
            await userRepo.RemoveUserFromDbAsync(It.IsAny<User>()).ConfigureAwait(false);
            dataAccessorMock.Verify(db => db.DeleteAsync(It.IsAny<User>(), null, null), Times.Once);
        }

        private async Task<bool> PermissionTest(string searchedPermission, IEnumerable<dynamic> userPermissions, IEnumerable<dynamic> rolePermissions)
        {
            var userSql = sqlProvider.GetSql("User.Permissions");
            var roleSql = sqlProvider.GetSql("User.RolePermissions");
            ulong userId = 1;
            dataAccessorMock
                .Setup(db => db.QueryAsync(userSql, It.IsAny<object>(), null, null, null))
                .Returns(Task.FromResult<IEnumerable<dynamic>>(userPermissions));

            dataAccessorMock
                .Setup(db => db.QueryAsync(roleSql, It.IsAny<object>(), null, null, null))
                .Returns(Task.FromResult(rolePermissions));
            var userRepo = new UserRepository(dataAccessorMock.Object, sqlProvider, appSettingsMock.Object);
            return await userRepo.IsAllowedAsync(searchedPermission, userId).ConfigureAwait(false);
        }
    }
}