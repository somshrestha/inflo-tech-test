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

    [Fact]
    public void Add_Get_ReturnsViewWithEmptyModel()
    {
        // Act
        var result = _controller.Add();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = (ViewResult)result;
        viewResult.Model.Should().BeOfType<UserViewModel>();
        var model = (UserViewModel)viewResult.Model;
        model.Forename.Should().BeEmpty();
        model.Forename.Should().BeEmpty();
        model.Forename.Should().BeEmpty();
        model.DateOfBirth.Should().BeNull();
        model.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Add_Post_ValidModel_CreatesUserAndRedirects()
    {
        // Arrange
        var model = new UserViewModel
        {
            Forename = "Test",
            Surname = "User",
            Email = "testuser@example.com",
            DateOfBirth = new DateTime(2000, 7, 15),
            IsActive = true
        };

        // Act
        var result = await _controller.Add(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = (RedirectToActionResult)result;
        redirectResult.ActionName.Should().Be("List");
    }

    [Fact]
    public async Task Add_Post_InvalidModel_ReturnsViewWithErrors()
    {
        // Arrange
        var model = new UserViewModel
        {
            Forename = "",
            Surname = "User",
            Email = "invalidemail",
            DateOfBirth = new DateTime(2000, 7, 15),
            IsActive = true
        };
        _controller.ModelState.AddModelError("Forename", "Forename is required.");
        _controller.ModelState.AddModelError("Email", "Invalid email format.");

        // Act
        var result = await _controller.Add(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = (ViewResult)result;
        viewResult.Model.Should().Be(model);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task View_ValidId_ReturnsViewResultWithUserViewModel()
    {
        // Arrange
        long id = 1;

        // Act
        var result = await _controller.View(id);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeOfType<UserViewModel>();
        var model = (UserViewModel)viewResult.Model;
        model.Id.Should().Be(1);
        model.Forename.Should().Be("Peter");
        model.Surname.Should().Be("Loew");
        model.Email.Should().Be("ploew@example.com");
        model.IsActive.Should().BeTrue();
        model.DateOfBirth.Should().Be(new DateTime(1988, 2, 11));
    }

    [Fact]
    public async Task View_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        long id = 999;

        // Act
        var result = await _controller.View(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
