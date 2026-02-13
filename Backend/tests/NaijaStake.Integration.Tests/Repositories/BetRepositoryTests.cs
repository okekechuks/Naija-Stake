using Xunit;
using FluentAssertions;
using NaijaStake.Domain.Entities;
using NaijaStake.Infrastructure.Repositories;
using NaijaStake.Integration.Tests.Fixtures;

namespace NaijaStake.Integration.Tests.Repositories;

public class BetRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private IBetRepository _betRepository = null!;

    public BetRepositoryTests()
    {
        _fixture = new DatabaseFixture();
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _betRepository = new BetRepository(_fixture.Context);
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_CreatesBet()
    {
        // Arrange
        var bet = Bet.Create("Bitcoin bet", "Will Bitcoin reach $100k?", BetCategory.Market,
            DateTime.UtcNow.AddHours(24), DateTime.UtcNow.AddHours(48),
            new[] { "Yes", "No" });

        // Act
        await _betRepository.AddAsync(bet);
        await _betRepository.SaveChangesAsync();

        // Assert
        var retrieved = await _betRepository.GetByIdAsync(bet.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Bitcoin bet");
    }

    [Fact]
    public async Task GetByCategoryAsync_FiltersByCategory()
    {
        // Arrange
        var bet1 = Bet.Create("Sports bet", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(24), DateTime.UtcNow.AddHours(48), new[] { "A", "B" });
        var bet2 = Bet.Create("Market bet", "Desc", BetCategory.Market,
            DateTime.UtcNow.AddHours(24), DateTime.UtcNow.AddHours(48), new[] { "A", "B" });

        await _betRepository.AddAsync(bet1);
        await _betRepository.AddAsync(bet2);
        await _betRepository.SaveChangesAsync();

        // Act
        var sports = await _betRepository.GetByCategoryAsync(BetCategory.Sports);
        var market = await _betRepository.GetByCategoryAsync(BetCategory.Market);

        // Assert
        sports.Should().HaveCount(1);
        sports.First().Title.Should().Be("Sports bet");
        market.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOpenBetsAsync_OnlyReturnsOpenBets()
    {
        // Arrange
        var openBet = Bet.Create("Open bet", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(24), DateTime.UtcNow.AddHours(48), new[] { "A", "B" });
        openBet.Open();

        var closedBet = Bet.Create("Closed bet", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(24), new[] { "A", "B" });
        closedBet.Open();
        closedBet.Close();

        await _betRepository.AddAsync(openBet);
        await _betRepository.AddAsync(closedBet);
        await _betRepository.SaveChangesAsync();

        // Act
        var openBets = await _betRepository.GetOpenBetsAsync();

        // Assert
        openBets.Should().HaveCount(1);
        openBets.First().Title.Should().Be("Open bet");
    }

    [Fact]
    public async Task GetWithOutcomesAsync_IncludesOutcomes()
    {
        // Arrange
        var bet = Bet.Create("Bet", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(24), DateTime.UtcNow.AddHours(48), 
            new[] { "Outcome A", "Outcome B", "Outcome C" });

        await _betRepository.AddAsync(bet);
        await _betRepository.SaveChangesAsync();

        // Act
        var retrieved = await _betRepository.GetWithOutcomesAsync(bet.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Outcomes.Should().HaveCount(3);
        retrieved.Outcomes.Should().ContainSingle(o => o.Title == "Outcome A");
    }
}
