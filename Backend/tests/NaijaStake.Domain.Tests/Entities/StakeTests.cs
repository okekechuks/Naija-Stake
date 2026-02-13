using Xunit;
using FluentAssertions;
using NaijaStake.Domain.Entities;
using NaijaStake.Domain.ValueObjects;

namespace NaijaStake.Domain.Tests.Entities;

public class StakeTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _betId = Guid.NewGuid();
    private readonly Guid _outcomeId = Guid.NewGuid();

    [Fact]
    public void Stake_Create_WithValidData_CreatesStake()
    {
        // Arrange & Act
        var stake = Stake.Create(_userId, _betId, _outcomeId, Money.From(500), "idempo-1");

        // Assert
        stake.UserId.Should().Be(_userId);
        stake.BetId.Should().Be(_betId);
        stake.OutcomeId.Should().Be(_outcomeId);
        stake.StakeAmount.Should().Be(Money.From(500));
        stake.Status.Should().Be(StakeStatus.Active);
        stake.IdempotencyKey.Should().Be("idempo-1");
    }

    [Fact]
    public void Stake_Create_WithEmptyUserId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Stake.Create(Guid.Empty, _betId, _outcomeId, Money.From(500), "idempo-1"));
    }

    [Fact]
    public void Stake_Create_WithNullStakeAmount_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            Stake.Create(_userId, _betId, _outcomeId, null!, "idempo-1"));
    }

    [Fact]
    public void Stake_MarkAsWon_SetsStatusAndPayout()
    {
        // Arrange
        var stake = Stake.Create(_userId, _betId, _outcomeId, Money.From(500), "idempo-1");
        var payout = Money.From(1200); // 500 stake + 700 winnings

        // Act
        stake.MarkAsWon(payout);

        // Assert
        stake.Status.Should().Be(StakeStatus.Won);
        stake.ActualPayout.Should().Be(payout);
        stake.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Stake_MarkAsWon_OnNonActiveStake_ThrowsException()
    {
        // Arrange
        var stake = Stake.Create(_userId, _betId, _outcomeId, Money.From(500), "idempo-1");
        stake.MarkAsLost();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            stake.MarkAsWon(Money.From(1000)));
    }

    [Fact]
    public void Stake_MarkAsWon_Idempotent_WithSameCall()
    {
        // Arrange
        var stake = Stake.Create(_userId, _betId, _outcomeId, Money.From(500), "idempo-1");
        var payout = Money.From(1200);
        
        stake.MarkAsWon(payout);
        var firstResolvedAt = stake.ResolvedAt;

        // Act - Call again
        stake.MarkAsWon(payout);

        // Assert - No exception, state unchanged
        stake.ResolvedAt.Should().Be(firstResolvedAt);
    }

    [Fact]
    public void Stake_MarkAsLost_SetsStatusToLost()
    {
        // Arrange
        var stake = Stake.Create(_userId, _betId, _outcomeId, Money.From(500), "idempo-1");

        // Act
        stake.MarkAsLost();

        // Assert
        stake.Status.Should().Be(StakeStatus.Lost);
        stake.ResolvedAt.Should().NotBeNull();
        stake.ActualPayout.Should().BeNull();
    }

    [Fact]
    public void Stake_MarkAsLost_Idempotent_WithSameCall()
    {
        // Arrange
        var stake = Stake.Create(_userId, _betId, _outcomeId, Money.From(500), "idempo-1");
        
        stake.MarkAsLost();
        var firstResolvedAt = stake.ResolvedAt;

        // Act - Call again
        stake.MarkAsLost();

        // Assert - No exception
        stake.ResolvedAt.Should().Be(firstResolvedAt);
    }

    [Fact]
    public void Stake_Cancel_SetsCancelledStatus()
    {
        // Arrange
        var stake = Stake.Create(_userId, _betId, _outcomeId, Money.From(500), "idempo-1");

        // Act
        stake.Cancel();

        // Assert
        stake.Status.Should().Be(StakeStatus.Cancelled);
        stake.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Stake_Cancel_Idempotent_WithSameCall()
    {
        // Arrange
        var stake = Stake.Create(_userId, _betId, _outcomeId, Money.From(500), "idempo-1");
        
        stake.Cancel();
        var firstResolvedAt = stake.ResolvedAt;

        // Act - Call again
        stake.Cancel();

        // Assert - No exception
        stake.ResolvedAt.Should().Be(firstResolvedAt);
    }
}
