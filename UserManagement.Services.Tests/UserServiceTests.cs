using System;
using System.Collections.Generic;
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
            new User { Id = 1, Forename = "Johnny", Surname = "User", Email = "juser@example.com", IsActive = true },
            new User { Id = 2, Forename = "John", Surname = "Doe", Email = "john.doe@example.com", IsActive = false },
            new User { Id = 3, Forename = "Smith", Surname = "Johnson", Email = "smith.johnson@example.com", IsActive = true }
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
}
