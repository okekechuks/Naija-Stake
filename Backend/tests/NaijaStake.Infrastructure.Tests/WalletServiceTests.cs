using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NaijaStake.Domain.Entities;
using NaijaStake.Infrastructure.Data;
using NaijaStake.Infrastructure.Repositories;
using NaijaStake.Infrastructure.Services;
using Xunit;

namespace NaijaStake.Infrastructure.Tests;

public class WalletServiceTests
{
    private StakeItDbContext CreateContext(SqliteConnection conn)
    {
        var options = new DbContextOptionsBuilder<StakeItDbContext>()
            .UseSqlite(conn)
            .Options;
        return new StakeItDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_Creates_Wallet_For_Existing_User()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        // prepare a user
        var user = User.Create("wtest@x.com", "08099999999", "h", "W", "User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var walletRepo = new WalletRepository(db);
        var userRepo = new UserRepository(db);
        var svc = new WalletService(walletRepo, userRepo);

        var wallet = await svc.CreateAsync(user.Id);

        wallet.Should().NotBeNull();
        wallet.UserId.Should().Be(user.Id);

        var fromDb = await db.Wallets.FindAsync(wallet.Id);
        fromDb.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_Throws_When_User_Not_Found()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var walletRepo = new WalletRepository(db);
        var userRepo = new UserRepository(db);
        var svc = new WalletService(walletRepo, userRepo);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByUserIdAsync_Returns_Wallet()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var user = User.Create("w2@x.com", "08011112222", "h2", "A", "B");
        await db.Users.AddAsync(user);
        var wallet = Wallet.Create(user.Id);
        await db.Wallets.AddAsync(wallet);
        await db.SaveChangesAsync();

        var walletRepo = new WalletRepository(db);
        var userRepo = new UserRepository(db);
        var svc = new WalletService(walletRepo, userRepo);

        var found = await svc.GetByUserIdAsync(user.Id);
        found.Should().NotBeNull();
        found!.Id.Should().Be(wallet.Id);
    }
}
