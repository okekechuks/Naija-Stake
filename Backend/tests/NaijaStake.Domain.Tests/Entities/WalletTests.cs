using Xunit;
using FluentAssertions;
using NaijaStake.Domain.Entities;
using NaijaStake.Domain.ValueObjects;

namespace NaijaStake.Domain.Tests.Entities;

public class WalletTests
{
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Wallet_Create_CreatesWalletWithZeroBalance()
    {
        // Arrange & Act
        var wallet = Wallet.Create(_userId);

        // Assert
        wallet.UserId.Should().Be(_userId);
        wallet.AvailableBalance.Should().Be(Money.Zero);
        wallet.LockedBalance.Should().Be(Money.Zero);
        wallet.TotalBalance.Should().Be(Money.Zero);
        wallet.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Wallet_RecordDeposit_IncreasesAvailableBalance()
    {
        // Arrange
        var wallet = Wallet.Create(_userId);
        var depositAmount = Money.From(1000);

        // Act
        wallet.RecordDeposit(depositAmount, "tx-001");

        // Assert
        wallet.AvailableBalance.Should().Be(depositAmount);
        wallet.LockedBalance.Should().Be(Money.Zero);
    }

    [Fact]
    public void Wallet_RecordStakeLocked_MovesFromAvailableToLocked()
    {
        // Arrange
        var wallet = Wallet.Create(_userId);
        wallet.RecordDeposit(Money.From(1000), "tx-001");
        var stakeAmount = Money.From(300);

        // Act
        wallet.RecordStakeLocked(stakeAmount, "tx-002");

        // Assert
        wallet.AvailableBalance.Should().Be(Money.From(700));
        wallet.LockedBalance.Should().Be(Money.From(300));
        wallet.TotalBalance.Should().Be(Money.From(1000));
    }

    [Fact]
    public void Wallet_RecordStakeLocked_WithInsufficientBalance_ThrowsException()
    {
        // Arrange
        var wallet = Wallet.Create(_userId);
        wallet.RecordDeposit(Money.From(200), "tx-001");
        var stakeAmount = Money.From(500);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            wallet.RecordStakeLocked(stakeAmount, "tx-002"));
    }

    [Fact]
    public void Wallet_RecordStakeRefund_MovesFromLockedToAvailable()
    {
        // Arrange
        var wallet = Wallet.Create(_userId);
        wallet.RecordDeposit(Money.From(1000), "tx-001");
        wallet.RecordStakeLocked(Money.From(300), "tx-002");

        // Act
        wallet.RecordStakeRefund(Money.From(300), "tx-003");

        // Assert
        wallet.AvailableBalance.Should().Be(Money.From(1000));
        wallet.LockedBalance.Should().Be(Money.Zero);
    }

    [Fact]
    public void Wallet_RecordStakeRefund_WithInsufficientLocked_ThrowsException()
    {
        // Arrange
        var wallet = Wallet.Create(_userId);
        wallet.RecordDeposit(Money.From(1000), "tx-001");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            wallet.RecordStakeRefund(Money.From(300), "tx-002"));
    }

    [Fact]
    public void Wallet_RecordWinPayout_IncreasesAvailableBalance()
    {
        // Arrange
        var wallet = Wallet.Create(_userId);
        wallet.RecordDeposit(Money.From(1000), "tx-001");
        wallet.RecordStakeLocked(Money.From(100), "tx-002");
        var payoutAmount = Money.From(250); // Stake (100) + Winnings (150)

        // Act
        wallet.RecordWinPayout(payoutAmount, "tx-003");

        // Assert
        wallet.AvailableBalance.Should().Be(Money.From(1150));
        wallet.LockedBalance.Should().Be(Money.Zero);
    }

    [Fact]
    public void Wallet_RecordPlatformFee_DeductsFromAvailable()
    {
        // Arrange
        var wallet = Wallet.Create(_userId);
        wallet.RecordDeposit(Money.From(1000), "tx-001");

        // Act
        wallet.RecordPlatformFee(Money.From(50), "tx-002");

        // Assert
        wallet.AvailableBalance.Should().Be(Money.From(950));
    }

    [Fact]
    public void Wallet_CanAfford_WithSufficientBalance_ReturnsTrue()
    {
        // Arrange
        var wallet = Wallet.Create(_userId);
        wallet.RecordDeposit(Money.From(1000), "tx-001");

        // Act
        var canAfford = wallet.CanAfford(Money.From(500));

        // Assert
        canAfford.Should().BeTrue();
    }

    [Fact]
    public void Wallet_CanAfford_WithInsufficientBalance_ReturnsFalse()
    {
        // Arrange
        var wallet = Wallet.Create(_userId);
        wallet.RecordDeposit(Money.From(300), "tx-001");

        // Act
        var canAfford = wallet.CanAfford(Money.From(500));

        // Assert
        canAfford.Should().BeFalse();
    }

    [Fact]
    public void Wallet_TotalBalance_IsAvailablePlusLocked()
    {
        // Arrange
        var wallet = Wallet.Create(_userId);
        wallet.RecordDeposit(Money.From(1000), "tx-001");
        wallet.RecordStakeLocked(Money.From(200), "tx-002");

        // Act & Assert
        wallet.TotalBalance.Should().Be(Money.From(1000));
        wallet.AvailableBalance.Should().Be(Money.From(800));
        wallet.LockedBalance.Should().Be(Money.From(200));
    }
}
