using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.Models;

namespace UserManagement.Data.Tests;

public class DataContextTests : IDisposable
{
    private readonly DataContext _dataContext;

    public DataContextTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: "UserManagement.Data.DataContext")
            .Options;
        _dataContext = new DataContext(options);
    }

    public void Dispose()
    {
        _dataContext.Database.EnsureDeleted();
        _dataContext.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllSeededUsers()
    {
        // Act
        var result = await _dataContext.GetAllAsync<User>();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(11);
    }

    [Fact]
    public async Task CreateAsync_AddsUserToDatabase()
    {
        // Arrange
        var newUser = new User
        {
            Id = 12,
            Forename = "Test",
            Surname = "User",
            Email = "test.user@example.com",
            IsActive = true,
            DateOfBirth = new DateTime(1990, 2, 18)
        };

        // Act
        await _dataContext.CreateAsync(newUser);
        var result = await _dataContext.GetAllAsync<User>();

        // Assert
        result.Should().HaveCount(12);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingUser()
    {
        // Arrange
        var existingUser = (await _dataContext.GetAllAsync<User>()).First(u => u.Id == 1);
        existingUser.Forename = "Updated";
        existingUser.Email = "updated.test@example.com";

        // Act
        await _dataContext.UpdateAsync(existingUser);
        var result = (await _dataContext.GetAllAsync<User>()).First(u => u.Id == 1);

        // Assert
        result.Forename.Should().Be("Updated");
        result.Email.Should().Be("updated.test@example.com");
    }

    [Fact]
    public async Task DeleteAsync_RemovesUserFromDatabase()
    {
        // Arrange
        var userToDelete = (await _dataContext.GetAllAsync<User>()).First(u => u.Id == 1);

        // Act
        await _dataContext.DeleteAsync(userToDelete);
        var result = await _dataContext.GetAllAsync<User>();

        // Assert
        result.Should().HaveCount(10);
        result.Should().NotContain(u => u.Id == 1);
    }

    [Fact]
    public async Task GetByIdAsync_GetsUserFromDatabase()
    {
        // Act
        var result = await _dataContext.GetByIdAsync<User>(1);

        // Assert
        result.Should().NotBeNull();
    }
    [Fact]
    public async Task GetByIdAsync_NonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _dataContext.GetByIdAsync<User>(999);

        // Assert
        result.Should().BeNull();
    }
}
