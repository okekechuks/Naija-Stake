using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NaijaStake.Domain.Entities;
using NaijaStake.Domain.ValueObjects;
using NaijaStake.Infrastructure.Data;
using NaijaStake.Infrastructure.Repositories;
using NaijaStake.Infrastructure.Services;
using Xunit;

namespace NaijaStake.Infrastructure.Tests;

public class TransactionServiceTests
{
    private StakeItDbContext CreateContext(SqliteConnection conn)
    {
        var options = new DbContextOptionsBuilder<StakeItDbContext>()
            .UseSqlite(conn)
            .Options;
        return new StakeItDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_Creates_Transaction_For_Valid_Wallet()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        // Setup: Create user and wallet
        var user = User.Create("tx1@test.com", "08011111111", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var wallet = Wallet.Create(user.Id);
        await db.Wallets.AddAsync(wallet);
        await db.SaveChangesAsync();

        // Create service
        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act: Create transaction
        var transaction = await svc.CreateAsync(
            wallet.Id,
            user.Id,
            TransactionType.Deposit,
            1000m,
            "Test deposit",
            idempotencyKey: "test-key-1");

        // Assert
        transaction.Should().NotBeNull();
        transaction.WalletId.Should().Be(wallet.Id);
        transaction.UserId.Should().Be(user.Id);
        transaction.Type.Should().Be(TransactionType.Deposit);
        transaction.Amount.Amount.Should().Be(1000m);
        transaction.Description.Should().Be("Test deposit");
        transaction.IdempotencyKey.Should().Be("test-key-1");
        transaction.Status.Should().Be(TransactionStatus.Completed);

        // Verify persisted
        var fromDb = await db.Transactions.FindAsync(transaction.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Amount.Amount.Should().Be(1000m);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_Wallet_Not_Found()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("tx2@test.com", "08022222222", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(
                Guid.NewGuid(), // Non-existent wallet
                user.Id,
                TransactionType.Deposit,
                1000m,
                "Test"));
    }

    [Fact]
    public async Task CreateAsync_Throws_When_Wallet_Does_Not_Belong_To_User()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        // Create two users
        var user1 = User.Create("tx3@test.com", "08033333333", "hash", "User", "One");
        var user2 = User.Create("tx4@test.com", "08044444444", "hash", "User", "Two");
        await db.Users.AddRangeAsync(user1, user2);
        await db.SaveChangesAsync();

        // Create wallet for user1
        var wallet = Wallet.Create(user1.Id);
        await db.Wallets.AddAsync(wallet);
        await db.SaveChangesAsync();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act & Assert: Try to create transaction with user2's ID but user1's wallet
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(
                wallet.Id,
                user2.Id, // Wrong user
                TransactionType.Deposit,
                1000m,
                "Test"));
    }

    [Fact]
    public async Task CreateAsync_Throws_When_Amount_Is_Zero()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("tx5@test.com", "08055555555", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var wallet = Wallet.Create(user.Id);
        await db.Wallets.AddAsync(wallet);
        await db.SaveChangesAsync();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(
                wallet.Id,
                user.Id,
                TransactionType.Deposit,
                0m, // Invalid amount
                "Test"));
    }

    [Fact]
    public async Task CreateAsync_Throws_When_Amount_Is_Negative()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("tx6@test.com", "08066666666", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var wallet = Wallet.Create(user.Id);
        await db.Wallets.AddAsync(wallet);
        await db.SaveChangesAsync();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(
                wallet.Id,
                user.Id,
                TransactionType.Deposit,
                -100m, // Invalid amount
                "Test"));
    }

    [Fact]
    public async Task CreateAsync_Throws_When_IdempotencyKey_Already_Exists()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("tx7@test.com", "08077777777", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var wallet = Wallet.Create(user.Id);
        await db.Wallets.AddAsync(wallet);
        await db.SaveChangesAsync();

        // Create first transaction with idempotency key
        var existingTransaction = Transaction.Create(
            wallet.Id,
            user.Id,
            TransactionType.Deposit,
            Money.From(500m),
            "First transaction",
            idempotencyKey: "duplicate-key");
        await db.Transactions.AddAsync(existingTransaction);
        await db.SaveChangesAsync();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act & Assert: Try to create another transaction with same idempotency key
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(
                wallet.Id,
                user.Id,
                TransactionType.Deposit,
                1000m,
                "Second transaction",
                idempotencyKey: "duplicate-key"));
    }

    [Fact]
    public async Task CreateAsync_Creates_Transaction_With_All_Optional_Parameters()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("tx8@test.com", "08088888888", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var wallet = Wallet.Create(user.Id);
        await db.Wallets.AddAsync(wallet);
        await db.SaveChangesAsync();

        var betId = Guid.NewGuid();
        var stakeId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act
        var transaction = await svc.CreateAsync(
            wallet.Id,
            user.Id,
            TransactionType.StakeLocked,
            250m,
            "Stake locked",
            betId: betId,
            stakeId: stakeId,
            paymentId: paymentId,
            idempotencyKey: "full-transaction-key",
            metadata: "{\"source\":\"test\"}");

        // Assert
        transaction.BetId.Should().Be(betId);
        transaction.StakeId.Should().Be(stakeId);
        transaction.PaymentId.Should().Be(paymentId);
        transaction.IdempotencyKey.Should().Be("full-transaction-key");
        transaction.Metadata.Should().Be("{\"source\":\"test\"}");
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Transaction_When_Exists()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("tx9@test.com", "08099999999", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var wallet = Wallet.Create(user.Id);
        await db.Wallets.AddAsync(wallet);
        await db.SaveChangesAsync();

        var transaction = Transaction.Create(
            wallet.Id,
            user.Id,
            TransactionType.Deposit,
            Money.From(500m),
            "Test deposit");
        await db.Transactions.AddAsync(transaction);
        await db.SaveChangesAsync();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act
        var found = await svc.GetByIdAsync(transaction.Id);

        // Assert
        found.Should().NotBeNull();
        found!.Id.Should().Be(transaction.Id);
        found.Amount.Amount.Should().Be(500m);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Exists()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act
        var found = await svc.GetByIdAsync(Guid.NewGuid());

        // Assert
        found.Should().BeNull();
    }

    [Fact]
    public async Task GetByWalletIdAsync_Returns_Transactions_Ordered_By_Recent_First()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("tx10@test.com", "08010101010", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var wallet = Wallet.Create(user.Id);
        await db.Wallets.AddAsync(wallet);
        await db.SaveChangesAsync();

        // Create multiple transactions
        var tx1 = Transaction.Create(wallet.Id, user.Id, TransactionType.Deposit, Money.From(100m), "First");
        var tx2 = Transaction.Create(wallet.Id, user.Id, TransactionType.Deposit, Money.From(200m), "Second");
        var tx3 = Transaction.Create(wallet.Id, user.Id, TransactionType.Deposit, Money.From(300m), "Third");
        await db.Transactions.AddRangeAsync(tx1, tx2, tx3);
        await db.SaveChangesAsync();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act
        var transactions = (await svc.GetByWalletIdAsync(wallet.Id)).ToList();

        // Assert
        transactions.Should().HaveCount(3);
        transactions[0].Description.Should().Be("Third"); // Most recent first
        transactions[1].Description.Should().Be("Second");
        transactions[2].Description.Should().Be("First");
    }

    [Fact]
    public async Task GetByWalletIdAsync_Respects_Limit()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("tx11@test.com", "08011111111", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var wallet = Wallet.Create(user.Id);
        await db.Wallets.AddAsync(wallet);
        await db.SaveChangesAsync();

        // Create 5 transactions
        for (int i = 1; i <= 5; i++)
        {
            var tx = Transaction.Create(wallet.Id, user.Id, TransactionType.Deposit, Money.From(i * 100m), $"Tx {i}");
            await db.Transactions.AddAsync(tx);
        }
        await db.SaveChangesAsync();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act: Request only 3 transactions
        var transactions = (await svc.GetByWalletIdAsync(wallet.Id, limit: 3)).ToList();

        // Assert
        transactions.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByUserIdAsync_Returns_Transactions_For_User()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user1 = User.Create("tx12@test.com", "08012121212", "hash", "User", "One");
        var user2 = User.Create("tx13@test.com", "08013131313", "hash", "User", "Two");
        await db.Users.AddRangeAsync(user1, user2);
        await db.SaveChangesAsync();

        var wallet1 = Wallet.Create(user1.Id);
        var wallet2 = Wallet.Create(user2.Id);
        await db.Wallets.AddRangeAsync(wallet1, wallet2);
        await db.SaveChangesAsync();

        // Create transactions for both users
        var tx1 = Transaction.Create(wallet1.Id, user1.Id, TransactionType.Deposit, Money.From(100m), "User1 tx");
        var tx2 = Transaction.Create(wallet2.Id, user2.Id, TransactionType.Deposit, Money.From(200m), "User2 tx");
        await db.Transactions.AddRangeAsync(tx1, tx2);
        await db.SaveChangesAsync();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act: Get transactions for user1
        var transactions = (await svc.GetByUserIdAsync(user1.Id)).ToList();

        // Assert
        transactions.Should().HaveCount(1);
        transactions[0].UserId.Should().Be(user1.Id);
        transactions[0].Description.Should().Be("User1 tx");
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_Returns_Transaction_When_Exists()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("tx14@test.com", "08014141414", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var wallet = Wallet.Create(user.Id);
        await db.Wallets.AddAsync(wallet);
        await db.SaveChangesAsync();

        var transaction = Transaction.Create(
            wallet.Id,
            user.Id,
            TransactionType.Deposit,
            Money.From(500m),
            "Test deposit",
            idempotencyKey: "test-idempotency-key");
        await db.Transactions.AddAsync(transaction);
        await db.SaveChangesAsync();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act
        var found = await svc.GetByIdempotencyKeyAsync("test-idempotency-key");

        // Assert
        found.Should().NotBeNull();
        found!.Id.Should().Be(transaction.Id);
        found.IdempotencyKey.Should().Be("test-idempotency-key");
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_Returns_Null_When_Not_Exists()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act
        var found = await svc.GetByIdempotencyKeyAsync("non-existent-key");

        // Assert
        found.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_Returns_Null_When_Key_Is_Null()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Act
        var found = await svc.GetByIdempotencyKeyAsync(null!);

        // Assert
        found.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_Supports_All_Transaction_Types()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("tx15@test.com", "08015151515", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var wallet = Wallet.Create(user.Id);
        await db.Wallets.AddAsync(wallet);
        await db.SaveChangesAsync();

        var transactionRepo = new TransactionRepository(db);
        var walletRepo = new WalletRepository(db);
        var svc = new TransactionService(transactionRepo, walletRepo);

        // Test all transaction types
        var types = Enum.GetValues<TransactionType>();
        foreach (var type in types)
        {
            var transaction = await svc.CreateAsync(
                wallet.Id,
                user.Id,
                type,
                100m,
                $"Test {type}",
                idempotencyKey: $"key-{type}");

            transaction.Type.Should().Be(type);
        }
    }
}
