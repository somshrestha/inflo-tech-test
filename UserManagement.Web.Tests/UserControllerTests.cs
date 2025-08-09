using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserManagement.Services.Domain.Implementations;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Web.Mapper;
using UserManagement.Web.Models.Users;
using UserManagement.Web.UserHelpers;
using UserManagement.WebMS.Controllers;

namespace UserManagement.Data.Tests;

public class UserControllerTests : IDisposable
{
    private readonly DataContext _dataContext;
    private readonly UsersController _controller;

    public UserControllerTests()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddConsole());
        services.AddDbContext<DataContext>(options =>
            options.UseInMemoryDatabase("UserManagement.Data.DataContext"));

        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

        services.AddScoped<IUserValidator, UserValidator>();

        var serviceProvider = services.BuildServiceProvider();
        _dataContext = serviceProvider.GetRequiredService<DataContext>();

        _dataContext.Database.EnsureCreated();

        var userService = new UserService(_dataContext);
        var mapper = serviceProvider.GetRequiredService<IMapper>();
        var userValidator = serviceProvider.GetRequiredService<IUserValidator>();
        _controller = new UsersController(userService, mapper, userValidator);
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
        var errorController = new UsersController(userServiceMock.Object, Mock.Of<IMapper>(), Mock.Of<IUserValidator>());

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
        viewResult.Model.Should().BeOfType<UserWithAuditViewModel>();
        var model = (UserWithAuditViewModel)viewResult.Model;
        model.User.Id.Should().Be(1);
        model.User.Forename.Should().Be("Peter");
        model.User.Surname.Should().Be("Loew");
        model.User.Email.Should().Be("ploew@example.com");
        model.User.IsActive.Should().BeTrue();
        model.User.DateOfBirth.Should().Be(new DateTime(1988, 2, 11));
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

    [Fact]
    public async Task EditGet_ValidId_ReturnsViewResultWithUserViewModel()
    {
        // Arrange
        long id = 1;

        // Act
        var result = await _controller.Edit(id);

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
    public async Task EditGet_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        long id = 999;

        // Act
        var result = await _controller.Edit(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task EditPost_ValidModel_UpdatesUserAndRedirects()
    {
        // Arrange
        var model = new UserViewModel
        {
            Id = 1,
            Forename = "Updated",
            Surname = "Loew",
            Email = "updated@example.com",
            IsActive = false,
            DateOfBirth = new DateTime(1988, 2, 11)
        };

        // Act
        var result = await _controller.Edit(1, model);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("List");
        var updatedUser = await _dataContext.Users!.FindAsync(1L);
        updatedUser!.Forename.Should().Be("Updated");
        updatedUser.Surname.Should().Be("Loew");
        updatedUser.Email.Should().Be("updated@example.com");
        updatedUser.IsActive.Should().BeFalse();
        updatedUser.DateOfBirth.Should().Be(new DateTime(1988, 2, 11));
    }

    [Fact]
    public async Task EditPost_InvalidModel_ReturnsViewWithModel()
    {
        // Arrange
        var model = new UserViewModel
        {
            Id = 1,
            Forename = "",
            Surname = "Loew",
            Email = "invalid",
            IsActive = true,
            DateOfBirth = DateTime.MinValue
        };
        _controller.ModelState.AddModelError("Forename", "Forename is required");
        _controller.ModelState.AddModelError("Email", "Invalid email address");

        // Act
        var result = await _controller.Edit(1, model);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeEquivalentTo(model);
        _controller.ModelState.ContainsKey("Forename").Should().BeTrue();
        _controller.ModelState.ContainsKey("Email").Should().BeTrue();
    }

    [Fact]
    public async Task EditPost_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var model = new UserViewModel
        {
            Id = 999,
            Forename = "Test",
            Surname = "User",
            Email = "test@example.com",
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        // Act
        var result = await _controller.Edit(999, model);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task EditPost_MismatchedId_ReturnsNotFound()
    {
        // Arrange
        var model = new UserViewModel
        {
            Id = 2,
            Forename = "Test",
            Surname = "User",
            Email = "test@example.com",
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        // Act
        var result = await _controller.Edit(1, model);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task DeleteGet_ValidId_ReturnsViewResultWithUserViewModel()
    {
        // Arrange
        long id = 1;

        // Act
        var result = await _controller.Delete(id);

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
    public async Task DeleteGet_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        long id = 999;

        // Act
        var result = await _controller.Delete(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteConfirmed_ValidId_DeletesUserAndRedirects()
    {
        // Arrange
        long id = 1;

        // Act
        var result = await _controller.DeleteConfirmed(id);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("List");
        var deletedUser = await _dataContext.Users!.FindAsync(1L);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task DeleteConfirmed_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        long id = 999;

        // Act
        var result = await _controller.DeleteConfirmed(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
