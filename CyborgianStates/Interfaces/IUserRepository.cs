﻿using CyborgianStates.Models;
using System.Threading.Tasks;

namespace CyborgianStates.Interfaces
{
    public interface IUserRepository
    {
        Task AddUserToDbAsync(ulong userId);

        Task<User> GetUserByExternalUserIdAsync(ulong externalUserId);

        Task<User> GetUserByIdAsync(ulong userId);

        Task<bool> IsAllowedAsync(string permissionType, ulong userId);

        Task<bool> IsUserInDbAsync(ulong userId);

        Task RemoveUserFromDbAsync(User user);
    }
}