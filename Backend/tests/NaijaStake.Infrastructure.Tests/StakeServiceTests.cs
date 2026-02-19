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

public class StakeServiceTests
{
    private StakeItDbContext CreateContext(SqliteConnection conn)
    {
        var options = new DbContextOptionsBuilder<StakeItDbContext>()
            .UseSqlite(conn)
            .Options;
        return new StakeItDbContext(options);
    }

    [Fact]
    public async Task PlaceStakeAsync_Creates_Stake_For_Valid_User_And_Bet()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        // Setup: Create user and bet
        var user = User.Create("stake1@test.com", "08011111111", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var bet = Bet.Create("Test Bet", "Description", BetCategory.Sports, closingTime, resolutionTime, new[] { "Option A", "Option B" });
        await db.Bets.AddAsync(bet);
        await db.SaveChangesAsync();

        var outcome = bet.Outcomes.First();
        var stakeAmount = Money.From(500m);
        var idempotencyKey = "test-key-1";

        var stakeRepo = new StakeRepository(db);
        var betRepo = new BetRepository(db);
        var userRepo = new UserRepository(db);
        var svc = new StakeService(stakeRepo, betRepo, userRepo);

        // Act
        var stake = await svc.PlaceStakeAsync(user.Id, bet.Id, outcome.Id, stakeAmount, idempotencyKey);

        // Assert
        stake.Should().NotBeNull();
        stake.UserId.Should().Be(user.Id);
        stake.BetId.Should().Be(bet.Id);
        stake.OutcomeId.Should().Be(outcome.Id);
        stake.StakeAmount.Amount.Should().Be(500m);
        stake.IdempotencyKey.Should().Be(idempotencyKey);
        stake.Status.Should().Be(StakeStatus.Active);

        // Verify persisted
        var fromDb = await db.Stakes.FindAsync(stake.Id);
        fromDb.Should().NotBeNull();
        fromDb!.StakeAmount.Amount.Should().Be(500m);
    }

    [Fact]
    public async Task PlaceStakeAsync_Throws_When_User_Not_Found()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var bet = Bet.Create("Test Bet", "Description", BetCategory.Sports, closingTime, resolutionTime, new[] { "Option A", "Option B" });
        await db.Bets.AddAsync(bet);
        await db.SaveChangesAsync();

        var outcome = bet.Outcomes.First();
        var stakeAmount = Money.From(500m);

        var stakeRepo = new StakeRepository(db);
        var betRepo = new BetRepository(db);
        var userRepo = new UserRepository(db);
        var svc = new StakeService(stakeRepo, betRepo, userRepo);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.PlaceStakeAsync(
                Guid.NewGuid(), // Non-existent user
                bet.Id,
                outcome.Id,
                stakeAmount,
                "test-key"));
    }

    [Fact]
    public async Task PlaceStakeAsync_Throws_When_Bet_Not_Found()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("stake2@test.com", "08022222222", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var stakeAmount = Money.From(500m);

        var stakeRepo = new StakeRepository(db);
        var betRepo = new BetRepository(db);
        var userRepo = new UserRepository(db);
        var svc = new StakeService(stakeRepo, betRepo, userRepo);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.PlaceStakeAsync(
                user.Id,
                Guid.NewGuid(), // Non-existent bet
                Guid.NewGuid(), // Non-existent outcome
                stakeAmount,
                "test-key"));
    }

    [Fact]
    public async Task PlaceStakeAsync_Creates_Stake_With_Different_Amounts()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("stake3@test.com", "08033333333", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var bet = Bet.Create("Test Bet", "Description", BetCategory.Sports, closingTime, resolutionTime, new[] { "Option A", "Option B" });
        await db.Bets.AddAsync(bet);
        await db.SaveChangesAsync();

        var outcome = bet.Outcomes.First();
        var stakeRepo = new StakeRepository(db);
        var betRepo = new BetRepository(db);
        var userRepo = new UserRepository(db);
        var svc = new StakeService(stakeRepo, betRepo, userRepo);

        // Test different amounts
        var amounts = new[] { 100m, 500m, 1000m, 5000m, 10000m };
        foreach (var amount in amounts)
        {
            var stake = await svc.PlaceStakeAsync(
                user.Id,
                bet.Id,
                outcome.Id,
                Money.From(amount),
                $"key-{amount}");

            stake.StakeAmount.Amount.Should().Be(amount);
        }
    }

    [Fact]
    public async Task PlaceStakeAsync_Creates_Stake_With_Different_Outcomes()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("stake4@test.com", "08044444444", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var bet = Bet.Create("Test Bet", "Description", BetCategory.Sports, closingTime, resolutionTime, new[] { "Option A", "Option B", "Option C" });
        await db.Bets.AddAsync(bet);
        await db.SaveChangesAsync();

        var stakeRepo = new StakeRepository(db);
        var betRepo = new BetRepository(db);
        var userRepo = new UserRepository(db);
        var svc = new StakeService(stakeRepo, betRepo, userRepo);

        // Place stakes on different outcomes
        foreach (var outcome in bet.Outcomes)
        {
            var stake = await svc.PlaceStakeAsync(
                user.Id,
                bet.Id,
                outcome.Id,
                Money.From(100m),
                $"key-{outcome.Id}");

            stake.OutcomeId.Should().Be(outcome.Id);
        }
    }

    [Fact]
    public async Task PlaceStakeAsync_Creates_Stake_With_Unique_IdempotencyKey()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("stake5@test.com", "08055555555", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var bet = Bet.Create("Test Bet", "Description", BetCategory.Sports, closingTime, resolutionTime, new[] { "Option A", "Option B" });
        await db.Bets.AddAsync(bet);
        await db.SaveChangesAsync();

        var outcome = bet.Outcomes.First();
        var stakeRepo = new StakeRepository(db);
        var betRepo = new BetRepository(db);
        var userRepo = new UserRepository(db);
        var svc = new StakeService(stakeRepo, betRepo, userRepo);

        // Act: Create multiple stakes with different idempotency keys
        var stake1 = await svc.PlaceStakeAsync(user.Id, bet.Id, outcome.Id, Money.From(100m), "key-1");
        var stake2 = await svc.PlaceStakeAsync(user.Id, bet.Id, outcome.Id, Money.From(200m), "key-2");
        var stake3 = await svc.PlaceStakeAsync(user.Id, bet.Id, outcome.Id, Money.From(300m), "key-3");

        // Assert
        stake1.IdempotencyKey.Should().Be("key-1");
        stake2.IdempotencyKey.Should().Be("key-2");
        stake3.IdempotencyKey.Should().Be("key-3");
        stake1.Id.Should().NotBe(stake2.Id);
        stake2.Id.Should().NotBe(stake3.Id);
    }

    [Fact]
    public async Task PlaceStakeAsync_Creates_Stake_For_Different_Users()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user1 = User.Create("stake6@test.com", "08066666666", "hash", "User", "One");
        var user2 = User.Create("stake7@test.com", "08077777777", "hash", "User", "Two");
        await db.Users.AddRangeAsync(user1, user2);
        await db.SaveChangesAsync();

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var bet = Bet.Create("Test Bet", "Description", BetCategory.Sports, closingTime, resolutionTime, new[] { "Option A", "Option B" });
        await db.Bets.AddAsync(bet);
        await db.SaveChangesAsync();

        var outcome = bet.Outcomes.First();
        var stakeRepo = new StakeRepository(db);
        var betRepo = new BetRepository(db);
        var userRepo = new UserRepository(db);
        var svc = new StakeService(stakeRepo, betRepo, userRepo);

        // Act: Both users place stakes
        var stake1 = await svc.PlaceStakeAsync(user1.Id, bet.Id, outcome.Id, Money.From(100m), "key-user1");
        var stake2 = await svc.PlaceStakeAsync(user2.Id, bet.Id, outcome.Id, Money.From(200m), "key-user2");

        // Assert
        stake1.UserId.Should().Be(user1.Id);
        stake2.UserId.Should().Be(user2.Id);
        stake1.Id.Should().NotBe(stake2.Id);
    }

    [Fact]
    public async Task PlaceStakeAsync_Creates_Stake_For_Different_Bets()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("stake8@test.com", "08088888888", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var bet1 = Bet.Create("Bet 1", "Description", BetCategory.Sports, closingTime, resolutionTime, new[] { "Option A", "Option B" });
        var bet2 = Bet.Create("Bet 2", "Description", BetCategory.Politics, closingTime, resolutionTime, new[] { "Option X", "Option Y" });
        await db.Bets.AddRangeAsync(bet1, bet2);
        await db.SaveChangesAsync();

        var outcome1 = bet1.Outcomes.First();
        var outcome2 = bet2.Outcomes.First();

        var stakeRepo = new StakeRepository(db);
        var betRepo = new BetRepository(db);
        var userRepo = new UserRepository(db);
        var svc = new StakeService(stakeRepo, betRepo, userRepo);

        // Act: Place stakes on different bets
        var stake1 = await svc.PlaceStakeAsync(user.Id, bet1.Id, outcome1.Id, Money.From(100m), "key-bet1");
        var stake2 = await svc.PlaceStakeAsync(user.Id, bet2.Id, outcome2.Id, Money.From(200m), "key-bet2");

        // Assert
        stake1.BetId.Should().Be(bet1.Id);
        stake2.BetId.Should().Be(bet2.Id);
        stake1.Id.Should().NotBe(stake2.Id);
    }

    [Fact]
    public async Task PlaceStakeAsync_Creates_Stake_With_Minimum_Amount()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("stake9@test.com", "08099999999", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var bet = Bet.Create("Test Bet", "Description", BetCategory.Sports, closingTime, resolutionTime, new[] { "Option A", "Option B" });
        await db.Bets.AddAsync(bet);
        await db.SaveChangesAsync();

        var outcome = bet.Outcomes.First();
        var stakeRepo = new StakeRepository(db);
        var betRepo = new BetRepository(db);
        var userRepo = new UserRepository(db);
        var svc = new StakeService(stakeRepo, betRepo, userRepo);

        // Act: Place stake with minimum amount (0.01)
        var stake = await svc.PlaceStakeAsync(user.Id, bet.Id, outcome.Id, Money.From(0.01m), "key-min");

        // Assert
        stake.StakeAmount.Amount.Should().Be(0.01m);
    }

    [Fact]
    public async Task PlaceStakeAsync_Creates_Stake_With_Large_Amount()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("stake10@test.com", "08010101010", "hash", "Test", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var bet = Bet.Create("Test Bet", "Description", BetCategory.Sports, closingTime, resolutionTime, new[] { "Option A", "Option B" });
        await db.Bets.AddAsync(bet);
        await db.SaveChangesAsync();

        var outcome = bet.Outcomes.First();
        var stakeRepo = new StakeRepository(db);
        var betRepo = new BetRepository(db);
        var userRepo = new UserRepository(db);
        var svc = new StakeService(stakeRepo, betRepo, userRepo);

        // Act: Place stake with large amount
        var largeAmount = 999999.99m;
        var stake = await svc.PlaceStakeAsync(user.Id, bet.Id, outcome.Id, Money.From(largeAmount), "key-large");

        // Assert
        stake.StakeAmount.Amount.Should().Be(largeAmount);
    }
}
