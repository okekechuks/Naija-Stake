using System;
using System.Linq;
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

public class BetServiceTests
{
    private StakeItDbContext CreateContext(SqliteConnection conn)
    {
        var options = new DbContextOptionsBuilder<StakeItDbContext>()
            .UseSqlite(conn)
            .Options;
        return new StakeItDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_Creates_Bet_With_Valid_Data()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var betRepo = new BetRepository(db);
        var svc = new BetService(betRepo);

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var outcomeOptions = new[] { "Option A", "Option B", "Option C" };

        // Act
        var bet = await svc.CreateAsync(
            "Test Bet Title",
            "Test Bet Description",
            BetCategory.Sports,
            closingTime,
            resolutionTime,
            outcomeOptions);

        // Assert
        bet.Should().NotBeNull();
        bet.Title.Should().Be("Test Bet Title");
        bet.Description.Should().Be("Test Bet Description");
        bet.Category.Should().Be(BetCategory.Sports);
        bet.Status.Should().Be(BetStatus.Draft);
        bet.ClosingTime.Should().BeCloseTo(closingTime, TimeSpan.FromSeconds(1));
        bet.ResolutionTime.Should().BeCloseTo(resolutionTime, TimeSpan.FromSeconds(1));
        bet.Outcomes.Should().HaveCount(3);
        bet.Outcomes.Select(o => o.Title).Should().BeEquivalentTo(outcomeOptions);
        bet.TotalStaked.Amount.Should().Be(0m);
        bet.ParticipantCount.Should().Be(0);

        // Verify persisted
        var fromDb = await db.Bets.FindAsync(bet.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Outcomes.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_Title_Is_Empty()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var betRepo = new BetRepository(db);
        var svc = new BetService(betRepo);

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var outcomeOptions = new[] { "Option A", "Option B" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(
                "", // Empty title
                "Description",
                BetCategory.Sports,
                closingTime,
                resolutionTime,
                outcomeOptions));
    }

    [Fact]
    public async Task CreateAsync_Throws_When_Title_Is_Whitespace()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var betRepo = new BetRepository(db);
        var svc = new BetService(betRepo);

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var outcomeOptions = new[] { "Option A", "Option B" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(
                "   ", // Whitespace title
                "Description",
                BetCategory.Sports,
                closingTime,
                resolutionTime,
                outcomeOptions));
    }

    [Fact]
    public async Task CreateAsync_Throws_When_Description_Is_Empty()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var betRepo = new BetRepository(db);
        var svc = new BetService(betRepo);

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var outcomeOptions = new[] { "Option A", "Option B" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(
                "Title",
                "", // Empty description
                BetCategory.Sports,
                closingTime,
                resolutionTime,
                outcomeOptions));
    }

    [Fact]
    public async Task CreateAsync_Throws_When_ResolutionTime_Is_Before_ClosingTime()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var betRepo = new BetRepository(db);
        var svc = new BetService(betRepo);

        var closingTime = DateTime.UtcNow.AddDays(2);
        var resolutionTime = DateTime.UtcNow.AddDays(1); // Before closing time
        var outcomeOptions = new[] { "Option A", "Option B" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(
                "Title",
                "Description",
                BetCategory.Sports,
                closingTime,
                resolutionTime,
                outcomeOptions));
    }

    [Fact]
    public async Task CreateAsync_Throws_When_ResolutionTime_Equals_ClosingTime()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var betRepo = new BetRepository(db);
        var svc = new BetService(betRepo);

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = closingTime; // Same as closing time
        var outcomeOptions = new[] { "Option A", "Option B" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(
                "Title",
                "Description",
                BetCategory.Sports,
                closingTime,
                resolutionTime,
                outcomeOptions));
    }

    [Fact]
    public async Task CreateAsync_Throws_When_OutcomeOptions_Is_Null()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var betRepo = new BetRepository(db);
        var svc = new BetService(betRepo);

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(
                "Title",
                "Description",
                BetCategory.Sports,
                closingTime,
                resolutionTime,
                null!)); // Null outcomes
    }

    [Fact]
    public async Task CreateAsync_Throws_When_OutcomeOptions_Is_Empty()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var betRepo = new BetRepository(db);
        var svc = new BetService(betRepo);

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(
                "Title",
                "Description",
                BetCategory.Sports,
                closingTime,
                resolutionTime,
                Array.Empty<string>())); // Empty outcomes
    }

    [Fact]
    public async Task CreateAsync_Throws_When_OutcomeOptions_Has_Less_Than_Two()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var betRepo = new BetRepository(db);
        var svc = new BetService(betRepo);

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(
                "Title",
                "Description",
                BetCategory.Sports,
                closingTime,
                resolutionTime,
                new[] { "Only One Option" })); // Only one outcome
    }

    [Fact]
    public async Task CreateAsync_Trims_Title_And_Description()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var betRepo = new BetRepository(db);
        var svc = new BetService(betRepo);

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var outcomeOptions = new[] { "Option A", "Option B" };

        // Act
        var bet = await svc.CreateAsync(
            "  Trimmed Title  ",
            "  Trimmed Description  ",
            BetCategory.Politics,
            closingTime,
            resolutionTime,
            outcomeOptions);

        // Assert
        bet.Title.Should().Be("Trimmed Title");
        bet.Description.Should().Be("Trimmed Description");
    }

    [Fact]
    public async Task CreateAsync_Supports_All_Categories()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var betRepo = new BetRepository(db);
        var svc = new BetService(betRepo);

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var outcomeOptions = new[] { "Option A", "Option B" };

        // Test all categories
        var categories = Enum.GetValues<BetCategory>();
        foreach (var category in categories)
        {
            var bet = await svc.CreateAsync(
                $"Test {category}",
                "Description",
                category,
                closingTime,
                resolutionTime,
                outcomeOptions);

            bet.Category.Should().Be(category);
        }
    }

    [Fact]
    public async Task GetOpenBetsAsync_Returns_Only_Open_Bets()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var betRepo = new BetRepository(db);
        var svc = new BetService(betRepo);

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var outcomeOptions = new[] { "Option A", "Option B" };

        // Create bets with different statuses
        var draftBet = await svc.CreateAsync("Draft Bet", "Description", BetCategory.Sports, closingTime, resolutionTime, outcomeOptions);
        
        var openBet = await svc.CreateAsync("Open Bet", "Description", BetCategory.Sports, closingTime, resolutionTime, outcomeOptions);
        openBet.Open();
        await db.SaveChangesAsync();

        // Create a closed bet with closing time in the past
        var pastClosingTime = DateTime.UtcNow.AddHours(-1);
        var pastResolutionTime = DateTime.UtcNow.AddDays(1);
        var closedBet = await svc.CreateAsync("Closed Bet", "Description", BetCategory.Sports, pastClosingTime, pastResolutionTime, outcomeOptions);
        closedBet.Open();
        closedBet.Close();
        await db.SaveChangesAsync();

        // Act
        var openBets = (await svc.GetOpenBetsAsync()).ToList();

        // Assert
        openBets.Should().HaveCount(1);
        openBets[0].Id.Should().Be(openBet.Id);
        openBets[0].Status.Should().Be(BetStatus.Open);
    }

    [Fact]
    public async Task GetOpenBetsAsync_Respects_Limit()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var betRepo = new BetRepository(db);
        var svc = new BetService(betRepo);

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var outcomeOptions = new[] { "Option A", "Option B" };

        // Create 5 open bets
        for (int i = 1; i <= 5; i++)
        {
            var bet = await svc.CreateAsync($"Bet {i}", "Description", BetCategory.Sports, closingTime, resolutionTime, outcomeOptions);
            bet.Open();
            await db.SaveChangesAsync();
        }

        // Act: Request only 3 bets
        var openBets = (await svc.GetOpenBetsAsync(limit: 3)).ToList();

        // Assert
        openBets.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetOpenBetsAsync_Returns_Empty_When_No_Open_Bets()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var betRepo = new BetRepository(db);
        var svc = new BetService(betRepo);

        var closingTime = DateTime.UtcNow.AddDays(1);
        var resolutionTime = DateTime.UtcNow.AddDays(2);
        var outcomeOptions = new[] { "Option A", "Option B" };

        // Create only draft bets
        await svc.CreateAsync("Draft Bet", "Description", BetCategory.Sports, closingTime, resolutionTime, outcomeOptions);

        // Act
        var openBets = (await svc.GetOpenBetsAsync()).ToList();

        // Assert
        openBets.Should().BeEmpty();
    }
}
