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

    private static StakeService CreateService(StakeItDbContext db)
    {
        return new StakeService(
            new StakeRepository(db),
            new BetRepository(db),
            new UserRepository(db),
            new WalletRepository(db),
            new TransactionRepository(db));
    }

    [Fact]
    public async Task PlaceStakeAsync_Locks_Wallet_Updates_Bet_And_Creates_Transaction()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("stake1@test.com", "08011111111", "hash", "Test", "User");
        var wallet = Wallet.Create(user.Id);
        wallet.RecordDeposit(Money.From(1000m), "seed-deposit");

        var bet = Bet.Create(
            "Test Bet",
            "Description",
            BetCategory.Sports,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            new[] { "Option A", "Option B" });
        bet.Open();

        await db.Users.AddAsync(user);
        await db.Wallets.AddAsync(wallet);
        await db.Bets.AddAsync(bet);
        await db.SaveChangesAsync();

        var outcome = bet.Outcomes.First();
        var svc = CreateService(db);

        var stake = await svc.PlaceStakeAsync(user.Id, bet.Id, outcome.Id, Money.From(250m), "stake-key-1");

        stake.Status.Should().Be(StakeStatus.Active);
        stake.StakeAmount.Amount.Should().Be(250m);

        var walletFromDb = await db.Wallets.FindAsync(wallet.Id);
        walletFromDb.Should().NotBeNull();
        walletFromDb!.AvailableBalance.Amount.Should().Be(750m);
        walletFromDb.LockedBalance.Amount.Should().Be(250m);

        var betFromDb = await db.Bets.Include(b => b.Outcomes).FirstAsync(b => b.Id == bet.Id);
        betFromDb.TotalStaked.Amount.Should().Be(250m);
        betFromDb.ParticipantCount.Should().Be(1);
        betFromDb.Outcomes.Single(o => o.Id == outcome.Id).TotalStaked.Amount.Should().Be(250m);
        betFromDb.Outcomes.Single(o => o.Id == outcome.Id).StakeCount.Should().Be(1);

        var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.StakeId == stake.Id);
        transaction.Should().NotBeNull();
        transaction!.Type.Should().Be(TransactionType.StakeLocked);
        transaction.Amount.Amount.Should().Be(250m);
        transaction.IdempotencyKey.Should().Be("stake-key-1");
    }

    [Fact]
    public async Task PlaceStakeAsync_Returns_Existing_Stake_For_Idempotent_Replay()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("stake2@test.com", "08022222222", "hash", "Test", "User");
        var wallet = Wallet.Create(user.Id);
        wallet.RecordDeposit(Money.From(1000m), "seed-deposit");

        var bet = Bet.Create(
            "Replay Bet",
            "Description",
            BetCategory.Sports,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            new[] { "Option A", "Option B" });
        bet.Open();

        await db.Users.AddAsync(user);
        await db.Wallets.AddAsync(wallet);
        await db.Bets.AddAsync(bet);
        await db.SaveChangesAsync();

        var outcome = bet.Outcomes.First();
        var svc = CreateService(db);

        var first = await svc.PlaceStakeAsync(user.Id, bet.Id, outcome.Id, Money.From(150m), "replay-key");
        var second = await svc.PlaceStakeAsync(user.Id, bet.Id, outcome.Id, Money.From(150m), "replay-key");

        second.Id.Should().Be(first.Id);
        second.StakeAmount.Amount.Should().Be(150m);

        (await db.Stakes.CountAsync()).Should().Be(1);
        (await db.Transactions.CountAsync()).Should().Be(1);

        var walletFromDb = await db.Wallets.FindAsync(wallet.Id);
        walletFromDb!.AvailableBalance.Amount.Should().Be(850m);
        walletFromDb.LockedBalance.Amount.Should().Be(150m);
    }

    [Fact]
    public async Task PlaceStakeAsync_Throws_When_Idempotency_Key_Is_Reused_For_Different_Request()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("stake3@test.com", "08033333333", "hash", "Test", "User");
        var wallet = Wallet.Create(user.Id);
        wallet.RecordDeposit(Money.From(1000m), "seed-deposit");

        var bet = Bet.Create(
            "Replay Guard Bet",
            "Description",
            BetCategory.Sports,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            new[] { "Option A", "Option B" });
        bet.Open();

        await db.Users.AddAsync(user);
        await db.Wallets.AddAsync(wallet);
        await db.Bets.AddAsync(bet);
        await db.SaveChangesAsync();

        var outcome = bet.Outcomes.First();
        var svc = CreateService(db);

        await svc.PlaceStakeAsync(user.Id, bet.Id, outcome.Id, Money.From(150m), "shared-key");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.PlaceStakeAsync(user.Id, bet.Id, outcome.Id, Money.From(200m), "shared-key"));
    }

    [Fact]
    public async Task PlaceStakeAsync_Throws_When_Wallet_Not_Found()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("stake4@test.com", "08044444444", "hash", "Test", "User");
        var bet = Bet.Create(
            "Wallet Bet",
            "Description",
            BetCategory.Sports,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            new[] { "Option A", "Option B" });
        bet.Open();

        await db.Users.AddAsync(user);
        await db.Bets.AddAsync(bet);
        await db.SaveChangesAsync();

        var outcome = bet.Outcomes.First();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.PlaceStakeAsync(user.Id, bet.Id, outcome.Id, Money.From(100m), "no-wallet-key"));
    }

    [Fact]
    public async Task PlaceStakeAsync_Throws_When_Wallet_Has_Insufficient_Balance()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("stake5@test.com", "08055555555", "hash", "Test", "User");
        var wallet = Wallet.Create(user.Id);
        wallet.RecordDeposit(Money.From(50m), "seed-deposit");

        var bet = Bet.Create(
            "Balance Bet",
            "Description",
            BetCategory.Sports,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            new[] { "Option A", "Option B" });
        bet.Open();

        await db.Users.AddAsync(user);
        await db.Wallets.AddAsync(wallet);
        await db.Bets.AddAsync(bet);
        await db.SaveChangesAsync();

        var outcome = bet.Outcomes.First();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.PlaceStakeAsync(user.Id, bet.Id, outcome.Id, Money.From(100m), "insufficient-key"));
    }

    [Fact]
    public async Task PlaceStakeAsync_Throws_When_Bet_Is_Not_Open()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("stake6@test.com", "08066666666", "hash", "Test", "User");
        var wallet = Wallet.Create(user.Id);
        wallet.RecordDeposit(Money.From(1000m), "seed-deposit");

        var bet = Bet.Create(
            "Draft Bet",
            "Description",
            BetCategory.Sports,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            new[] { "Option A", "Option B" });

        await db.Users.AddAsync(user);
        await db.Wallets.AddAsync(wallet);
        await db.Bets.AddAsync(bet);
        await db.SaveChangesAsync();

        var outcome = bet.Outcomes.First();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.PlaceStakeAsync(user.Id, bet.Id, outcome.Id, Money.From(100m), "draft-key"));
    }

    [Fact]
    public async Task PlaceStakeAsync_Throws_When_Outcome_Does_Not_Belong_To_Bet()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("stake7@test.com", "08077777777", "hash", "Test", "User");
        var wallet = Wallet.Create(user.Id);
        wallet.RecordDeposit(Money.From(1000m), "seed-deposit");

        var bet = Bet.Create(
            "Primary Bet",
            "Description",
            BetCategory.Sports,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            new[] { "Option A", "Option B" });
        bet.Open();

        var otherBet = Bet.Create(
            "Other Bet",
            "Description",
            BetCategory.Politics,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            new[] { "Option X", "Option Y" });
        otherBet.Open();

        await db.Users.AddAsync(user);
        await db.Wallets.AddAsync(wallet);
        await db.Bets.AddRangeAsync(bet, otherBet);
        await db.SaveChangesAsync();

        var foreignOutcome = otherBet.Outcomes.First();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.PlaceStakeAsync(user.Id, bet.Id, foreignOutcome.Id, Money.From(100m), "wrong-outcome-key"));
    }
}
