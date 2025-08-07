using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Domain.Implementations;

namespace UserManagement.Data.Tests;

public class UserServiceTests
{
    private readonly Mock<IDataContext> _dataContextMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _dataContextMock = new Mock<IDataContext>();
        _userService = new UserService(_dataContextMock.Object);
    }

    private static IEnumerable<User> GetSampleUsers()
    {
        return new List<User>
        {
            new User { Id = 1, Forename = "Johnny", Surname = "User", Email = "juser@example.com", IsActive = true, DateOfBirth = new DateTime(1990, 4, 21) },
            new User { Id = 2, Forename = "John", Surname = "Doe", Email = "john.doe@example.com", IsActive = false, DateOfBirth = new DateTime(1988, 8, 11) },
            new User { Id = 3, Forename = "Smith", Surname = "Johnson", Email = "smith.johnson@example.com", IsActive = true, DateOfBirth = new DateTime(1998, 7, 1) }
        };
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange
        var users = GetSampleUsers();
        _dataContextMock.Setup(dc => dc.GetAllAsync<User>()).ReturnsAsync(users);

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(users);
        _dataContextMock.Verify(dc => dc.GetAllAsync<User>(), Times.Once);
    }

    [Fact]
    public async Task FilterByActive_WhenIsActiveTrue_ReturnsOnlyActiveUsers()
    {
        // Arrange
        var users = GetSampleUsers();
        _dataContextMock.Setup(dc => dc.GetAllAsync<User>()).ReturnsAsync(users);

        // Act
        var result = await _userService.FilterByActive(true);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(u => u.IsActive.Should().BeTrue());
        _dataContextMock.Verify(dc => dc.GetAllAsync<User>(), Times.Once);
    }

    [Fact]
    public async Task FilterByActive_WhenIsActiveFalse_ReturnsOnlyInactiveUsers()
    {
        // Arrange
        var users = GetSampleUsers();
        _dataContextMock.Setup(dc => dc.GetAllAsync<User>()).ReturnsAsync(users);

        // Act
        var result = await _userService.FilterByActive(false);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().AllSatisfy(u => u.IsActive.Should().BeFalse());
        _dataContextMock.Verify(dc => dc.GetAllAsync<User>(), Times.Once);
    }

    [Fact]
    public async Task FilterByActive_WhenIsActiveNull_ReturnsAllUsers()
    {
        // Arrange
        var users = GetSampleUsers();
        _dataContextMock.Setup(dc => dc.GetAllAsync<User>()).ReturnsAsync(users);

        // Act
        var result = await _userService.FilterByActive(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(users);
        _dataContextMock.Verify(dc => dc.GetAllAsync<User>(), Times.Once);
    }

    [Fact]
    public async Task FilterByActive_WhenNoUsers_ReturnsEmptyCollection()
    {
        // Arrange
        var users = new List<User>();
        _dataContextMock.Setup(dc => dc.GetAllAsync<User>()).ReturnsAsync(users);

        // Act
        var result = await _userService.FilterByActive(true);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _dataContextMock.Verify(dc => dc.GetAllAsync<User>(), Times.Once);
    }

    [Fact]
    public async Task FilterByActive_WhenDataContextThrowsException_PropagatesException()
    {
        // Arrange
        var exceptionMessage = "Data access error";
        _dataContextMock.Setup(dc => dc.GetAllAsync<User>()).ThrowsAsync(new Exception(exceptionMessage));

        // Act & Assert
        await FluentActions.Invoking(() => _userService.FilterByActive(true))
            .Should().ThrowAsync<Exception>().WithMessage(exceptionMessage);
        _dataContextMock.Verify(dc => dc.GetAllAsync<User>(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ValidUser_CallsAddAndSaveChanges()
    {
        // Arrange
        var user = new User
        {
            Forename = "Test",
            Surname = "User",
            Email = "test.user@example.com",
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        _dataContextMock.Setup(dc => dc.CreateAsync(It.IsAny<User>())).Verifiable();

        // Act
        await _userService.CreateAsync(user);

        // Assert
        _dataContextMock.Verify(dc => dc.CreateAsync(user), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenDataContextThrowsException_PropagatesException()
    {
        // Arrange
        var user = new User { Forename = "Test", Surname = "User" };
        _dataContextMock.Setup(dc => dc.CreateAsync(It.IsAny<User>())).ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await FluentActions.Invoking(() => _userService.CreateAsync(user))
            .Should().ThrowAsync<Exception>().WithMessage("Database error");
        _dataContextMock.Verify(dc => dc.CreateAsync(user), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var users = GetSampleUsers();
        var expectedUser = users.First(u => u.Id == 1);
        _dataContextMock.Setup(dc => dc.GetByIdAsync<User>(1L)).ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedUser);
        _dataContextMock.Verify(dc => dc.GetByIdAsync<User>(1L), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        _dataContextMock.Setup(dc => dc.GetByIdAsync<User>(999L)).ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
        _dataContextMock.Verify(dc => dc.GetByIdAsync<User>(999L), Times.Once);
    }
}
