using Xunit;
using FluentAssertions;
using NaijaStake.Domain.Entities;
using NaijaStake.Domain.ValueObjects;
using NaijaStake.Infrastructure.Repositories;
using NaijaStake.Integration.Tests.Fixtures;

namespace NaijaStake.Integration.Tests.Repositories;

public class WalletRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private IWalletRepository _walletRepository = null!;

    public WalletRepositoryTests()
    {
        _fixture = new DatabaseFixture();
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _walletRepository = new WalletRepository(_fixture.Context);
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_CreatesWallet()
    {
        // Arrange
        var user = User.Create("test@example.com", "+1234567890", "hash", "Test", "User");
        _fixture.Context.Users.Add(user);
        await _fixture.Context.SaveChangesAsync();

        var wallet = Wallet.Create(user.Id);

        // Act
        await _walletRepository.AddAsync(wallet);
        await _walletRepository.SaveChangesAsync();

        // Assert
        var retrieved = await _walletRepository.GetByIdAsync(wallet.Id);
        retrieved.Should().NotBeNull();
        retrieved!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_FindsWallet()
    {
        // Arrange
        var user = User.Create("test2@example.com", "+1234567891", "hash2", "Test2", "User2");
        _fixture.Context.Users.Add(user);
        await _fixture.Context.SaveChangesAsync();

        var wallet = Wallet.Create(user.Id);
        await _walletRepository.AddAsync(wallet);
        await _walletRepository.SaveChangesAsync();

        // Act
        var retrieved = await _walletRepository.GetByUserIdAsync(user.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(wallet.Id);
    }

    [Fact]
    public async Task GetWithTransactionsAsync_IncludesTransactions()
    {
        // Arrange
        var user = User.Create("test3@example.com", "+1234567892", "hash3", "Test3", "User3");
        _fixture.Context.Users.Add(user);
        await _fixture.Context.SaveChangesAsync();

        var wallet = Wallet.Create(user.Id);
        await _walletRepository.AddAsync(wallet);
        await _walletRepository.SaveChangesAsync();

        var transaction = Transaction.Create(
            wallet.Id, user.Id, TransactionType.Deposit, Money.From(1000),
            "Deposit transaction");

        // Note: In real app, we'd use transaction repository
        _fixture.Context.Transactions.Add(transaction);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var retrieved = await _walletRepository.GetWithTransactionsAsync(wallet.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Transactions.Should().HaveCount(1);
    }
}
