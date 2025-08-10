using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserManagement.Data;
using UserManagement.Data.Interceptors;
using UserManagement.Models;
using UserManagement.Services.Domain.Domain.Implementations;
using UserManagement.Web.Controllers;
using UserManagement.Web.Mapper;
using UserManagement.Web.Models.AuditLogs;

namespace UserManagement.Web.Tests;
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

        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

        var serviceProvider = services.BuildServiceProvider();
        _dataContext = serviceProvider.GetRequiredService<DataContext>();

        _dataContext.Database.EnsureCreated();

        var auditLogService = new AuditLogsService(_dataContext);
        var mapper = serviceProvider.GetRequiredService<IMapper>();
        _controller = new AuditLogsController(auditLogService, mapper);
    }

    public void Dispose()
    {
        _dataContext.Database.EnsureDeleted();
        _dataContext.Dispose();
    }

    [Fact]
    public async Task Index_NoFilters_ReturnsEmptyListWhenNoAuditLogs()
    {
        // Act
        var result = await _controller.Index(page: 1, pageSize: 10, search: null, actionType: null, sortDescending: true);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = (ViewResult)result;
        viewResult.Model.Should().NotBeNull();
        var model = (AuditLogListViewModel)viewResult.Model;
        model.Items.Should().BeEmpty();
        model.CurrentPage.Should().Be(1);
        model.PageSize.Should().Be(10);
        model.TotalItems.Should().Be(0);
        model.SearchQuery.Should().BeNull();
        model.ActionTypeFilter.Should().BeNull();
        model.SortDescending.Should().BeTrue();
    }

    [Fact]
    public async Task Index_WithAuditLogs_ReturnsPagedList()
    {
        // Arrange
        // Create a user to generate an audit log via the interceptor
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
        var result = await _controller.Index(page: 1, pageSize: 10, search: null, actionType: null, sortDescending: true);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = (ViewResult)result;
        viewResult.Model.Should().BeOfType<AuditLogListViewModel>();
        var model = (AuditLogListViewModel)viewResult.Model;
        model.Items.Should().HaveCount(1);
        model.TotalItems.Should().Be(1);
        model.Items.First().UserId.Should().Be(user.Id);
        model.Items.First().ActionType.Should().Be("Create");
        model.Items.First().Details.Should().Be($"User Test User created with email test@example.com");
        model.CurrentPage.Should().Be(1);
        model.PageSize.Should().Be(10);
        model.SortDescending.Should().BeTrue();
    }

    [Fact]
    public async Task Index_WithSearchFilter_ReturnsFilteredLogs()
    {
        // Arrange
        // Create two audit logs
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
        var result = await _controller.Index(page: 1, pageSize: 10, search: "Test", actionType: null, sortDescending: true);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = (ViewResult)result;
        viewResult.Model.Should().NotBeNull();
        var model = (AuditLogListViewModel)viewResult.Model;
        model.Items.Should().HaveCount(1);
        model.TotalItems.Should().Be(1);
        model.Items.First().UserId.Should().Be(user1.Id);
        model.Items.First().Details.Should().Contain("Test User");
        model.SearchQuery.Should().Be("Test");
    }

    [Fact]
    public async Task Index_WithActionTypeFilter_ReturnsFilteredLogs()
    {
        // Arrange
        // Create a user (generates Create log)
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

        // Update the user (generates Update log)
        user.Forename = "Updated";
        _dataContext.Users.Update(user);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _controller.Index(page: 1, pageSize: 10, search: null, actionType: "Create", sortDescending: true);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = (ViewResult)result;
        viewResult.Model.Should().NotBeNull();
        var model = (AuditLogListViewModel)viewResult.Model;
        model.Items.Should().HaveCount(1);
        model.TotalItems.Should().Be(1);
        model.Items.First().ActionType.Should().Be("Create");
        model.ActionTypeFilter.Should().Be("Create");
    }

    [Fact]
    public async Task Index_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        // Create 15 audit logs
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
        var result = await _controller.Index(page: 2, pageSize: 5, search: null, actionType: null, sortDescending: true);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = (ViewResult)result;
        viewResult.Model.Should().NotBeNull();
        var model = (AuditLogListViewModel)viewResult.Model;
        model.Items.Should().HaveCount(5);
        model.CurrentPage.Should().Be(2);
        model.PageSize.Should().Be(5);
        model.TotalItems.Should().Be(15);
    }

    [Fact]
    public async Task Details_ValidId_ReturnsViewWithAuditLog()
    {
        // Arrange
        // Create a user to generate an audit log
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
        var result = await _controller.Details(auditLog.Id);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = (ViewResult)result;
        viewResult.Model.Should().BeOfType<AuditLogViewModel>();
        var model = (AuditLogViewModel)viewResult.Model;
        model.Id.Should().Be(auditLog.Id);
        model.UserId.Should().Be(user.Id);
        model.ActionType.Should().Be("Create");
        model.Details.Should().Be("User Test User created with email test@example.com");
    }

    [Fact]
    public async Task Details_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        long id = 999;

        // Act
        var result = await _controller.Details(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
