using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Services.Domain.Implementations;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Web.Models.Users;
using UserManagement.WebMS.Controllers;

namespace UserManagement.Data.Tests;

public class UserControllerTests : IDisposable
{
    private readonly DataContext _dataContext;
    private readonly UsersController _controller;

    public UserControllerTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<DataContext>(options =>
            options.UseInMemoryDatabase("UserManagement.Data.DataContext"));

        var serviceProvider = services.BuildServiceProvider();
        _dataContext = serviceProvider.GetRequiredService<DataContext>();

        _dataContext.Database.EnsureCreated();

        var userService = new UserService(_dataContext);
        _controller = new UsersController(userService);
    }

    public void Dispose()
    {
        _dataContext.Database.EnsureDeleted();
        _dataContext.Dispose();
    }

    [Fact]
    public async Task List_NoFilter_ReturnsAllUsers()
    {
        // Act
        var result = await _controller.List(null);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = (ViewResult)result;
        viewResult.Model.Should().BeOfType<UserListViewModel>();
        var model = (UserListViewModel)viewResult.Model;
        model.Items.Should().HaveCount(11);
        model.Items.Should().Contain(item => item.Forename == "Peter" && item.Surname == "Loew" && item.DateOfBirth == new DateTime(1988, 2, 11));
    }

    [Fact]
    public async Task List_ActiveFilterTrue_ReturnsOnlyActiveUsers()
    {
        // Act
        var result = await _controller.List(true);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = (ViewResult)result;
        viewResult.Model.Should().BeOfType<UserListViewModel>();
        var model = (UserListViewModel)viewResult.Model;
        model.Items.Should().HaveCount(7);
        model.Items.Should().OnlyContain(item => item.IsActive);
    }

    [Fact]
    public async Task List_ActiveFilterFalse_ReturnsOnlyInactiveUsers()
    {
        // Act
        var result = await _controller.List(false);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = (ViewResult)result;
        viewResult.Model.Should().BeOfType<UserListViewModel>();
        var model = (UserListViewModel)viewResult.Model;
        model.Items.Should().HaveCount(4);
        model.Items.Should().OnlyContain(item => !item.IsActive);
    }

    [Fact]
    public async Task List_DatabaseError_ReturnsStatusCode500()
    {
        // Arrange
        var userServiceMock = new Mock<IUserService>();
        userServiceMock.Setup(s => s.FilterByActive(It.IsAny<bool?>()))
            .ThrowsAsync(new Exception("Simulated database error"));
        var errorController = new UsersController(userServiceMock.Object);

        // Act
        var result = await errorController.List(null);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = (ObjectResult)result;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while retrieving the user list.");
    }
}
