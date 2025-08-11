using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserManagement.Api.Controllers;
using UserManagement.Data;
using UserManagement.Data.Interceptors;
using UserManagement.Services.Domain.Domain.Implementations;
using UserManagement.UI.Models;
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

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();

        services.AddDbContext<DataContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TestConnectionForAudits"))
                   .AddInterceptors(new AuditSaveChangesInterceptor()));

        var serviceProvider = services.BuildServiceProvider();
        _dataContext = serviceProvider.GetRequiredService<DataContext>();

        _dataContext.Database.Migrate();

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
    public async Task GetById_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        long id = 999;
        var exceptionMessage = "User with ID 999 not found.";

        // Act && Assert
        await FluentActions.Invoking(() => _controller.GetById(id))
            .Should().ThrowAsync<KeyNotFoundException>().WithMessage(exceptionMessage);
    }
}
