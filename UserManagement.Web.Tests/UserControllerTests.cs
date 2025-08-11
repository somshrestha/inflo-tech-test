using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserManagement.Api.Controllers;
using UserManagement.Data;
using UserManagement.Data.Interceptors;
using UserManagement.Models;
using UserManagement.Services.Domain.Implementations;

namespace UserManagement.Api.Tests;

public class UsersControllerTests : IDisposable
{
    private readonly DataContext _dataContext;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();

        services.AddDbContext<DataContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TestConnectionForUsers"))
                   .AddInterceptors(new AuditSaveChangesInterceptor()));

        var serviceProvider = services.BuildServiceProvider();
        _dataContext = serviceProvider.GetRequiredService<DataContext>();

        _dataContext.Database.Migrate();

        var userService = new UserService(_dataContext);
        _controller = new UsersController(userService);
    }

    public void Dispose()
    {
        _dataContext.Database.EnsureDeleted();
        _dataContext.Dispose();
    }

    [Fact]
    public async Task GetAll_ReturnsAllUsers()
    {
        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var users = okResult!.Value as IEnumerable<User>;
        users.Should().HaveCount(11);
    }

    [Fact]
    public async Task FilterByActive_True_ReturnsOnlyActiveUsers()
    {
        // Act
        var result = await _controller.FilterByActive(true);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var users = okResult!.Value as IEnumerable<User>;
        users.Should().HaveCount(7);
        users!.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task FilterByActive_False_ReturnsOnlyInactiveUsers()
    {
        // Act
        var result = await _controller.FilterByActive(false);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var users = okResult!.Value as IEnumerable<User>;
        users.Should().HaveCount(4);
        users!.First().IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetById_ValidId_ReturnsUser()
    {
        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var user = okResult!.Value as User;
        user.Should().NotBeNull();
        user!.Id.Should().Be(1);
        user.Forename.Should().Be("Peter");
        user.Surname.Should().Be("Loew");
        user.Email.Should().Be("ploew@example.com");
        user.IsActive.Should().BeTrue();
        user.DateOfBirth.Should().Be(new DateTime(1988, 2, 11));
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ValidModel_CreatesUserAndReturnsCreated()
    {
        // Arrange
        var newUser = new User
        {
            Forename = "Test",
            Surname = "User",
            Email = "testuser@example.com",
            IsActive = true,
            DateOfBirth = new DateTime(2000, 7, 15)
        };

        // Act
        var result = await _controller.Create(newUser);

        // Assert
        var createdResult = result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.ActionName.Should().Be("GetById");
        createdResult.RouteValues!["id"].Should().Be(newUser.Id);
        var returnedUser = createdResult.Value as User;
        returnedUser.Should().BeEquivalentTo(newUser);

        var dbUser = await _dataContext.Users.FindAsync(newUser.Id);
        dbUser.Should().BeEquivalentTo(newUser);
    }

    [Fact]
    public async Task Update_ValidModel_UpdatesUserAndReturnsNoContent()
    {
        // Arrange
        var updatedUser = new User
        {
            Id = 1,
            Forename = "Updated",
            Surname = "Loew",
            Email = "updated@example.com",
            IsActive = false,
            DateOfBirth = new DateTime(1988, 2, 11)
        };

        // Act
        var result = await _controller.Update(1, updatedUser);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var dbUser = await _dataContext.Users.FindAsync(1L);
        dbUser.Should().BeEquivalentTo(updatedUser);
    }

    [Fact]
    public async Task Update_MismatchedId_ReturnsBadRequest()
    {
        // Arrange
        var user = new User { Id = 2 /* mismatch */ };

        // Act
        var result = await _controller.Update(1, user);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Update_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var user = new User { Id = 999 };

        // Act
        var result = await _controller.Update(999, user);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ValidId_DeletesUserAndReturnsNoContent()
    {
        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var deletedUser = await _dataContext.Users.FindAsync(1L);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NonExistentId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.Delete(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
