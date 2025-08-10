using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserManagement.Api.Controllers;
using UserManagement.Data;
using UserManagement.Data.Interceptors;
using UserManagement.Services.Domain.Domain.Implementations;
using UserManagement.UI.Models;
using AuditLog = UserManagement.Data.Entities.AuditLog;
using User = UserManagement.Models.User;

namespace UserManagement.Api.Tests;

public class AuditLogsControllerTests : IDisposable
{
    private readonly DataContext _dataContext;
    private readonly AuditLogsController _controller;

    public AuditLogsControllerTests()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddConsole());

        services.AddDbContext<DataContext>(options =>
            options.UseInMemoryDatabase($"UserManagement.Data.DataContext_{Guid.NewGuid()}")
                   .AddInterceptors(new AuditSaveChangesInterceptor()));

        var serviceProvider = services.BuildServiceProvider();
        _dataContext = serviceProvider.GetRequiredService<DataContext>();

        _dataContext.Database.EnsureCreated();

        var auditLogService = new AuditLogsService(_dataContext);
        _controller = new AuditLogsController(auditLogService);
    }

    public void Dispose()
    {
        _dataContext.Database.EnsureDeleted();
        _dataContext.Dispose();
    }

    [Fact]
    public async Task GetAll_NoFilters_ReturnsEmptyListWhenNoAuditLogs()
    {
        // Act
        var result = await _controller.GetAll(page: 1, pageSize: 10, search: null, actionType: null, sortDescending: true);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        var response = JsonSerializer.Deserialize<AuditLogResponse>(JsonSerializer.Serialize(okResult.Value));
        response.Should().NotBeNull();
        response!.Logs.Should().BeEmpty();
        response.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetAll_WithAuditLogs_ReturnsPagedList()
    {
        // Arrange
        var user = new User
        {
            Forename = "Test",
            Surname = "User",
            Email = "test@example.com",
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        await _dataContext.Users.AddAsync(user);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(page: 1, pageSize: 10, search: null, actionType: null, sortDescending: true);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        var response = JsonSerializer.Deserialize<AuditLogResponse>(JsonSerializer.Serialize(okResult.Value));
        response.Should().NotBeNull();
        response!.Logs.Should().HaveCount(1);
        response.Total.Should().Be(1);
        var firstLog = response.Logs.First();
        firstLog.UserId.Should().Be(user.Id);
        firstLog.ActionType.Should().Be("Create");
        firstLog.Details.Should().Be($"User Test User created with email test@example.com");
    }

    [Fact]
    public async Task GetAll_WithSearchFilter_ReturnsFilteredLogs()
    {
        // Arrange
        var user1 = new User
        {
            Forename = "Test",
            Surname = "User",
            Email = "test@example.com",
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        var user2 = new User
        {
            Forename = "Another",
            Surname = "Person",
            Email = "another@example.com",
            IsActive = true,
            DateOfBirth = new DateTime(1991, 1, 1)
        };
        await _dataContext.Users.AddRangeAsync(user1, user2);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(page: 1, pageSize: 10, search: "Test", actionType: null, sortDescending: true);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        var response = JsonSerializer.Deserialize<AuditLogResponse>(JsonSerializer.Serialize(okResult.Value));
        response.Should().NotBeNull();
        response!.Logs.Should().HaveCount(1);
        response.Total.Should().Be(1);
        var firstLog = response.Logs.First();
        firstLog.UserId.Should().Be(user1.Id);
        firstLog.Details.Should().Contain("Test User");
    }

    [Fact]
    public async Task GetAll_WithActionTypeFilter_ReturnsFilteredLogs()
    {
        // Arrange
        var user = new User
        {
            Forename = "Test",
            Surname = "User",
            Email = "test@example.com",
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        await _dataContext.Users.AddAsync(user);
        await _dataContext.SaveChangesAsync();

        user.Forename = "Updated";
        _dataContext.Users.Update(user);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(page: 1, pageSize: 10, search: null, actionType: "Create", sortDescending: true);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        var response = JsonSerializer.Deserialize<AuditLogResponse>(JsonSerializer.Serialize(okResult.Value));
        response.Should().NotBeNull();
        response!.Logs.Should().HaveCount(1);
        response.Total.Should().Be(1);
        var firstLog = response.Logs.First();
        firstLog.ActionType.Should().Be("Create");
    }

    [Fact]
    public async Task GetAll_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            var user = new User
            {
                Forename = $"Test{i}",
                Surname = "User",
                Email = $"test{i}@example.com",
                IsActive = true,
                DateOfBirth = new DateTime(1990, 1, 1)
            };
            await _dataContext.Users.AddAsync(user);
            await _dataContext.SaveChangesAsync();
        }

        // Act
        var result = await _controller.GetAll(page: 2, pageSize: 5, search: null, actionType: null, sortDescending: true);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        var response = JsonSerializer.Deserialize<AuditLogResponse>(JsonSerializer.Serialize(okResult.Value));
        response.Should().NotBeNull();
        response!.Logs.Should().HaveCount(5);
        response.Total.Should().Be(15);
    }

    [Fact]
    public async Task GetById_ValidId_ReturnsAuditLog()
    {
        // Arrange
        var user = new User
        {
            Forename = "Test",
            Surname = "User",
            Email = "test@example.com",
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        await _dataContext.Users.AddAsync(user);
        await _dataContext.SaveChangesAsync();

        var auditLog = await _dataContext.AuditLogs.FirstAsync();

        // Act
        var result = await _controller.GetById(auditLog.Id);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        var model = okResult.Value as AuditLog;
        model.Should().NotBeNull();
        model!.Id.Should().Be(auditLog.Id);
        model.UserId.Should().Be(user.Id);
        model.ActionType.Should().Be("Create");
        model.Details.Should().Be("User Test User created with email test@example.com");
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        long id = 999;

        // Act
        var result = await _controller.GetById(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
