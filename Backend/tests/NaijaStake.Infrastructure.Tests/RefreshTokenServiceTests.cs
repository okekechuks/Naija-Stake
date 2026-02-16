using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NaijaStake.Infrastructure.Data;
using NaijaStake.Infrastructure.Services;
using Xunit;

namespace NaijaStake.Infrastructure.Tests;

public class RefreshTokenServiceTests
{
    private StakeItDbContext CreateContext(SqliteConnection conn)
    {
        var options = new DbContextOptionsBuilder<StakeItDbContext>()
            .UseSqlite(conn)
            .Options;
        return new StakeItDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_Creates_RefreshToken()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var svc = new RefreshTokenService(db);
        var userId = Guid.NewGuid();
        var rt = await svc.CreateAsync(userId, TimeSpan.FromMinutes(60));

        rt.Should().NotBeNull();
        rt.UserId.Should().Be(userId);
        rt.Token.Should().NotBeNullOrEmpty();
        rt.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // persisted
        var fromDb = await db.RefreshTokens.FindAsync(rt.Id);
        fromDb.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByTokenAsync_Returns_Token()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var svc = new RefreshTokenService(db);
        var userId = Guid.NewGuid();
        var rt = await svc.CreateAsync(userId, TimeSpan.FromMinutes(60));

        var found = await svc.GetByTokenAsync(rt.Token);
        found.Should().NotBeNull();
        found!.Id.Should().Be(rt.Id);
    }

    [Fact]
    public async Task RevokeAsync_Sets_RevokedAt()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var svc = new RefreshTokenService(db);
        var userId = Guid.NewGuid();
        var rt = await svc.CreateAsync(userId, TimeSpan.FromMinutes(60));

        await svc.RevokeAsync(rt.Id);

        var fromDb = await db.RefreshTokens.FindAsync(rt.Id);
        fromDb.Should().NotBeNull();
        fromDb!.IsRevoked.Should().BeTrue();
        fromDb.RevokedAt.Should().NotBeNull();
    }
}
