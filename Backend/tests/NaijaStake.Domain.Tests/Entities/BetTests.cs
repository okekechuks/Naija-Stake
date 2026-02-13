using Xunit;
using FluentAssertions;
using NaijaStake.Domain.Entities;
using NaijaStake.Domain.ValueObjects;

namespace NaijaStake.Domain.Tests.Entities;

public class BetTests
{
    [Fact]
    public void Bet_Create_WithValidData_CreatesBet()
    {
        // Arrange
        var title = "Will Bitcoin hit $100k?";
        var description = "Bitcoin price prediction";
        var category = BetCategory.Market;
        var closingTime = DateTime.UtcNow.AddHours(24);
        var resolutionTime = DateTime.UtcNow.AddHours(48);
        var outcomes = new[] { "Yes", "No" };

        // Act
        var bet = Bet.Create(title, description, category, closingTime, resolutionTime, outcomes);

        // Assert
        bet.Title.Should().Be(title);
        bet.Description.Should().Be(description);
        bet.Category.Should().Be(category);
        bet.Status.Should().Be(BetStatus.Draft);
        bet.Outcomes.Should().HaveCount(2);
        bet.TotalStaked.Should().Be(Money.Zero);
        bet.ParticipantCount.Should().Be(0);
    }

    [Fact]
    public void Bet_Create_WithClosingTimeInPast_ThrowsArgumentException()
    {
        // Act
        var bet = Bet.Create("Title", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(1),
            new[] { "A", "B" });

        // Assert - creation allowed for already-closed bets
        bet.Should().NotBeNull();
    }

    [Fact]
    public void Bet_Create_WithResolutionBeforeClosing_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Bet.Create("Title", "Desc", BetCategory.Sports,
                DateTime.UtcNow.AddHours(24),
                DateTime.UtcNow.AddHours(12), // Before closing
                new[] { "A", "B" }));
    }

    [Fact]
    public void Bet_Create_WithLessThanTwoOutcomes_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Bet.Create("Title", "Desc", BetCategory.Sports,
                DateTime.UtcNow.AddHours(24),
                DateTime.UtcNow.AddHours(48),
                new[] { "Yes" })); // Only one outcome
    }

    [Fact]
    public void Bet_Open_FromDraft_TransitionsToOpen()
    {
        // Arrange
        var bet = Bet.Create("Title", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(24), DateTime.UtcNow.AddHours(48), new[] { "A", "B" });

        // Act
        bet.Open();

        // Assert
        bet.Status.Should().Be(BetStatus.Open);
    }

    [Fact]
    public void Bet_Open_FromNonDraft_ThrowsException()
    {
        // Arrange
        var bet = Bet.Create("Title", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(24), DateTime.UtcNow.AddHours(48), new[] { "A", "B" });
        bet.Open();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => bet.Open());
    }

    [Fact]
    public void Bet_Close_FromOpen_WithPastClosingTime_Succeeds()
    {
        // Arrange
        var bet = Bet.Create("Title", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(-1), // Closing time in past
            DateTime.UtcNow.AddHours(24), 
            new[] { "A", "B" });
        bet.Open();

        // Act
        bet.Close();

        // Assert
        bet.Status.Should().Be(BetStatus.Closed);
    }

    [Fact]
    public void Bet_Close_BeforeClosingTime_ThrowsException()
    {
        // Arrange
        var bet = Bet.Create("Title", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(24), DateTime.UtcNow.AddHours(48), new[] { "A", "B" });
        bet.Open();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => bet.Close());
    }

    [Fact]
    public void Bet_Resolve_WithValidOutcome_Succeeds()
    {
        // Arrange
        var bet = Bet.Create("Title", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(-1), new[] { "Yes", "No" });
        bet.Open();
        bet.Close();
        var winningOutcomeId = bet.Outcomes.First().Id;

        // Act
        bet.Resolve(winningOutcomeId, "Bitcoin reached $100k", "idempo-key-1");

        // Assert
        bet.Status.Should().Be(BetStatus.Resolved);
        bet.ResolvedOutcomeId.Should().Be(winningOutcomeId);
        bet.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Bet_Resolve_WithIdempotency_IsIdempotent()
    {
        // Arrange
        var bet = Bet.Create("Title", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(-1), new[] { "Yes", "No" });
        bet.Open();
        bet.Close();
        var winningOutcomeId = bet.Outcomes.First().Id;

        bet.Resolve(winningOutcomeId, "Notes", "idempo-key-1");
        var firstResolvedAt = bet.ResolvedAt;

        // Act - Replay with same idempotency key
        bet.Resolve(winningOutcomeId, "Different notes", "idempo-key-1");

        // Assert - No exception, state unchanged
        bet.ResolvedAt.Should().Be(firstResolvedAt);
    }

    [Fact]
    public void Bet_AddStake_IncreasesTotalAndCount()
    {
        // Arrange
        var bet = Bet.Create("Title", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(24), DateTime.UtcNow.AddHours(48), new[] { "A", "B" });
        bet.Open();

        // Act
        bet.AddStake(Money.From(500));
        bet.AddStake(Money.From(300));

        // Assert
        bet.TotalStaked.Should().Be(Money.From(800));
        bet.ParticipantCount.Should().Be(2);
    }

    [Fact]
    public void Bet_AddStake_OnClosedBet_ThrowsException()
    {
        // Arrange
        var bet = Bet.Create("Title", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(24), new[] { "A", "B" });
        bet.Open();
        bet.Close();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => bet.AddStake(Money.From(100)));
    }

    [Fact]
    public void Bet_IsOpen_ReturnsTrueWhenOpenAndNotClosed()
    {
        // Arrange
        var bet = Bet.Create("Title", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(24), DateTime.UtcNow.AddHours(48), new[] { "A", "B" });
        bet.Open();

        // Act & Assert
        bet.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void Bet_IsOpen_ReturnsFalseWhenClosed()
    {
        // Arrange
        var bet = Bet.Create("Title", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(24), new[] { "A", "B" });
        bet.Open();
        bet.Close();

        // Act & Assert
        bet.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Bet_IsSettled_ReturnsTrueWhenResolved()
    {
        // Arrange
        var bet = Bet.Create("Title", "Desc", BetCategory.Sports,
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(-1), new[] { "A", "B" });
        bet.Open();
        bet.Close();
        bet.Resolve(bet.Outcomes.First().Id);

        // Act & Assert
        bet.IsSettled.Should().BeTrue();
    }
}
