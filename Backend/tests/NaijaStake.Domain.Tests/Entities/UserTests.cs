using Xunit;
using FluentAssertions;
using NaijaStake.Domain.Entities;
using NaijaStake.Domain.ValueObjects;

namespace NaijaStake.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void User_Create_WithValidData_CreatesUser()
    {
        // Arrange & Act
        var user = User.Create("john@example.com", "1234567890", "password_hash", "John", "Doe");

        // Assert
        user.Email.Should().Be("john@example.com");
        user.PhoneNumber.Should().Be("1234567890");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Status.Should().Be(UserStatus.Active);
        user.Id.Should().NotBe(Guid.Empty);
        user.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void User_Create_WithInvalidEmail_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            User.Create("invalid-email", "1234567890", "hash", "John", "Doe"));
    }

    [Fact]
    public void User_Create_WithEmptyPhoneNumber_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            User.Create("john@example.com", "", "hash", "John", "Doe"));
    }

    [Fact]
    public void User_Email_NormalizedToLowercase()
    {
        // Arrange & Act
        var user = User.Create("JOHN@EXAMPLE.COM", "1234567890", "hash", "John", "Doe");

        // Assert
        user.Email.Should().Be("john@example.com");
    }

    [Fact]
    public void User_RecordLogin_UpdatesLastLoginAt()
    {
        // Arrange
        var user = User.Create("john@example.com", "1234567890", "hash", "John", "Doe");
        var before = DateTime.UtcNow;

        // Act
        user.RecordLogin();
        var after = DateTime.UtcNow;

        // Assert
        user.LastLoginAt.Should().BeOnOrAfter(before);
        user.LastLoginAt.Should().BeOnOrBefore(after);
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void User_Deactivate_SetsStatusToInactive()
    {
        // Arrange
        var user = User.Create("john@example.com", "1234567890", "hash", "John", "Doe");

        // Act
        user.Deactivate();

        // Assert
        user.Status.Should().Be(UserStatus.Inactive);
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void User_Reactivate_SetsStatusToActive()
    {
        // Arrange
        var user = User.Create("john@example.com", "1234567890", "hash", "John", "Doe");
        user.Deactivate();

        // Act
        user.Reactivate();

        // Assert
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void User_GetFullName_ReturnsFirstAndLastName()
    {
        // Arrange
        var user = User.Create("john@example.com", "1234567890", "hash", "John", "Doe");

        // Act
        var fullName = user.GetFullName();

        // Assert
        fullName.Should().Be("John Doe");
    }
}
