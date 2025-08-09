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

        var auditLog = new AuditLog
        {
            UserId = user.Id,
            ActionType = "Create",
            Timestamp = DateTime.UtcNow,
            Details = $"User {user.Forename} {user.Surname} created with email {user.Email}"
        };
        await _dataContext.CreateAsync(auditLog);
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        return await _dataContext.GetByIdAsync<User>(id);
    }

    public async Task UpdateAsync(User user)
    {
        await _dataContext.UpdateAsync(user);

        var auditLog = new AuditLog
        {
            UserId = user.Id,
            ActionType = "Update",
            Timestamp = DateTime.UtcNow,
            Details = $"User {user.Forename} {user.Surname} updated with email {user.Email}, IsActive: {user.IsActive}"
        };
        await _dataContext.CreateAsync(auditLog);
    }

    public async Task DeleteAsync(User user)
    {
        await _dataContext.DeleteAsync(user);

        var auditLog = new AuditLog
        {
            UserId = user.Id,
            ActionType = "Delete",
            Timestamp = DateTime.UtcNow,
            Details = $"User {user.Forename} {user.Surname} deleted with email {user.Email}"
        };
        await _dataContext.CreateAsync(auditLog);
    }

    public async Task<IEnumerable<AuditLog>> GetUserAuditLogs(long userId)
    {
        return await _dataContext.AuditLogs
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync();
    }
}
