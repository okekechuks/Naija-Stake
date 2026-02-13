using Xunit;
using FluentAssertions;
using NaijaStake.Domain.Entities;
using NaijaStake.Infrastructure.Repositories;
using NaijaStake.Integration.Tests.Fixtures;

namespace NaijaStake.Integration.Tests.Repositories;

public class UserRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private IUserRepository _userRepository = null!;

    public UserRepositoryTests()
    {
        _fixture = new DatabaseFixture();
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _userRepository = new UserRepository(_fixture.Context);
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_CreatesUser()
    {
        // Arrange
        var user = User.Create("john@example.com", "1234567890", "hash", "John", "Doe");

        // Act
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // Assert
        var retrieved = await _userRepository.GetByIdAsync(user.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_FindsUser()
    {
        // Arrange
        var user = User.Create("john@example.com", "1234567890", "hash", "John", "Doe");
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // Act
        var retrieved = await _userRepository.GetByEmailAsync("john@example.com");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_WithUppercaseEmail_NormalizesAndFinds()
    {
        // Arrange
        var user = User.Create("john@example.com", "1234567890", "hash", "John", "Doe");
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // Act
        var retrieved = await _userRepository.GetByEmailAsync("JOHN@EXAMPLE.COM");

        // Assert
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task EmailExistsAsync_ReturnsTrueWhenExists()
    {
        // Arrange
        var user = User.Create("john@example.com", "1234567890", "hash", "John", "Doe");
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // Act
        var exists = await _userRepository.EmailExistsAsync("john@example.com");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_ReturnsFalseWhenNotExists()
    {
        // Act
        var exists = await _userRepository.EmailExistsAsync("notfound@example.com");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetByPhoneNumberAsync_FindsUser()
    {
        // Arrange
        var user = User.Create("john@example.com", "1234567890", "hash", "John", "Doe");
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // Act
        var retrieved = await _userRepository.GetByPhoneNumberAsync("1234567890");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetWithWalletAsync_IncludesWallet()
    {
        // Arrange
        var user = User.Create("john@example.com", "1234567890", "hash", "John", "Doe");
        var wallet = Wallet.Create(user.Id);
        user.Wallet = wallet;

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // Act
        var retrieved = await _userRepository.GetWithWalletAsync(user.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Wallet.Should().NotBeNull();
        retrieved.Wallet.UserId.Should().Be(user.Id);
    }
}
