using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Models;

namespace UserManagement.Services.Domain.Interfaces;

public interface IUserService 
{
    /// <summary>
    /// Return users by active state
    /// </summary>
    /// <param name="isActive"></param>
    /// <returns></returns>
    Task<IEnumerable<User>> FilterByActive(bool? isActive);

    /// <summary>
    /// Return all users
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<User>> GetAllAsync();

    /// <summary>
    /// Adds user
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task CreateAsync(User user);

    /// <summary>
    /// Gets an user by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<User?> GetByIdAsync(long id);

    /// <summary>
    /// Updates user
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task UpdateAsync(User user);

    /// <summary>
    /// Deletes user
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task DeleteAsync(User user);
}
