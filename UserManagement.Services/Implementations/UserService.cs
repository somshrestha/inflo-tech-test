using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Models;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

public class UserService : IUserService
{
    private readonly IDataContext _dataContext;
    public UserService(IDataContext dataContext) => _dataContext = dataContext;

    public async Task<IEnumerable<User>> FilterByActive(bool? isActive)
    {
        var users = await _dataContext.GetAllAsync<User>();

        return isActive.HasValue
            ? users.Where(u => u.IsActive == isActive)
            : users;
    }

    public async Task<IEnumerable<User>> GetAllAsync() => await _dataContext.GetAllAsync<User>();

    public async Task CreateAsync(User user)
    {
        await _dataContext.CreateAsync(user);
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        return await _dataContext.GetByIdAsync<User>(id);
    }

    public async Task UpdateAsync(User user)
    {
        var existingUser = await _dataContext.GetByIdAsync<User>(user.Id);
        if (existingUser == null)
            throw new InvalidOperationException($"User with ID {user.Id} not found.");

        existingUser.Forename = user.Forename;
        existingUser.Surname = user.Surname;
        existingUser.Email = user.Email;
        existingUser.IsActive = user.IsActive;
        existingUser.DateOfBirth = user.DateOfBirth;

        await _dataContext.UpdateAsync(existingUser);
    }

    public async Task DeleteAsync(User user)
    {
        await _dataContext.DeleteAsync(user);
    }

    public async Task<IEnumerable<AuditLog>> GetUserAuditLogs(long userId)
    {
        return await _dataContext.AuditLogs
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync();
    }
}
