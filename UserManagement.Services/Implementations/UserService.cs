using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserManagement.Data;
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
        await _dataContext.UpdateAsync(user);
    }

    public async Task DeleteAsync(User user)
    {
        await _dataContext.DeleteAsync(user);
    }
}
