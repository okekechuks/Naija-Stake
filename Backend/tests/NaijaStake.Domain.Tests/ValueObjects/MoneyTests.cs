using Xunit;
using FluentAssertions;
using NaijaStake.Domain.ValueObjects;

namespace NaijaStake.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Money_From_CreatesMoneyWithDecimalAmount()
    {
        // Arrange & Act
        var money = Money.From(100.50m);

        // Assert
        money.Amount.Should().Be(100.50m);
    }

    [Fact]
    public void Money_From_WithNegativeAmount_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => Money.From(-50));
    }

    [Fact]
    public void Money_Zero_ReturnsZero()
    {
        // Arrange & Act
        var zero = Money.Zero;

        // Assert
        zero.Amount.Should().Be(0);
    }

    [Fact]
    public void Money_Add_ReturnsNewMoneyWithSum()
    {
        // Arrange
        var money1 = Money.From(100);
        var money2 = Money.From(50);

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.Should().Be(150);
        money1.Amount.Should().Be(100); // Original unchanged
    }

    [Fact]
    public void Money_Subtract_ReturnsNewMoneyWithDifference()
    {
        // Arrange
        var money1 = Money.From(100);
        var money2 = Money.From(30);

        // Act
        var result = money1.Subtract(money2);

        // Assert
        result.Amount.Should().Be(70);
        money1.Amount.Should().Be(100); // Original unchanged
    }

    [Fact]
    public void Money_Subtract_WhenInsufficientFunds_ThrowsInvalidOperationException()
    {
        // Arrange
        var money1 = Money.From(50);
        var money2 = Money.From(100);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => money1.Subtract(money2));
    }

    [Fact]
    public void Money_IsGreaterThanOrEqualTo_WithGreaterAmount_ReturnsTrue()
    {
        // Arrange
        var money1 = Money.From(100);
        var money2 = Money.From(50);

        // Act
        var result = money1.IsGreaterThanOrEqualTo(money2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Money_IsGreaterThanOrEqualTo_WithEqualAmount_ReturnsTrue()
    {
        // Arrange
        var money1 = Money.From(100);
        var money2 = Money.From(100);

        // Act
        var result = money1.IsGreaterThanOrEqualTo(money2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Money_IsLessThanOrEqualTo_WithLesserAmount_ReturnsTrue()
    {
        // Arrange
        var money1 = Money.From(50);
        var money2 = Money.From(100);

        // Act
        var result = money1.IsLessThanOrEqualTo(money2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Money_Equality_WithSameAmount_ReturnsTrue()
    {
        // Arrange
        var money1 = Money.From(100);
        var money2 = Money.From(100);

        // Act & Assert
        (money1 == money2).Should().BeTrue();
        money1.Equals(money2).Should().BeTrue();
    }

    [Fact]
    public void Money_Inequality_WithDifferentAmount_ReturnsTrue()
    {
        // Arrange
        var money1 = Money.From(100);
        var money2 = Money.From(50);

        // Act & Assert
        (money1 != money2).Should().BeTrue();
    }

    [Fact]
    public void Money_OperatorGreater_Works()
    {
        // Arrange
        var money1 = Money.From(100);
        var money2 = Money.From(50);

        // Act & Assert
        (money1 > money2).Should().BeTrue();
    }

    [Fact]
    public void Money_OperatorLess_Works()
    {
        // Arrange
        var money1 = Money.From(50);
        var money2 = Money.From(100);

        // Act & Assert
        (money1 < money2).Should().BeTrue();
    }

    [Fact]
    public void Money_DecimalPrecision_Preserved()
    {
        // Arrange & Act
        var money = Money.From(123.456789m); // More than 2 decimal places
        var result = Money.From(money.Amount);

        // Assert - Should preserve up to system precision
        // In real usage, DB will truncate to 2 decimals
        result.Amount.Should().Be(123.456789m);
    }
}
