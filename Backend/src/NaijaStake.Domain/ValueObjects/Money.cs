namespace NaijaStake.Domain.ValueObjects;

/// <summary>
/// Value object for monetary amounts. Uses decimal to avoid floating-point precision issues.
/// Always immutable. This is critical for financial transactions.
/// </summary>
public sealed class Money : IEquatable<Money>, IComparable<Money>
{
        public decimal Amount { get; private set; }

        // Parameterless constructor required by EF Core for materialization
        private Money()
        {
            Amount = 0m;
        }

        private Money(decimal amount)
        {
            if (amount < 0)
                throw new ArgumentException("Money amount cannot be negative.", nameof(amount));

            Amount = amount;
        }

    public static Money From(decimal amount) => new(amount);
    public static Money Zero => new(0);

    // Critical: Immutable arithmetic operations that return new Money instances
    public Money Add(Money other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        return new Money(Amount + other.Amount);
    }

    public Money Subtract(Money other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        if (other.Amount > Amount)
            throw new InvalidOperationException("Cannot subtract more than available balance.");

        return new Money(Amount - other.Amount);
    }

    public bool IsGreaterThanOrEqualTo(Money other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        return Amount >= other.Amount;
    }

    public bool IsLessThanOrEqualTo(Money other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        return Amount <= other.Amount;
    }

    public override bool Equals(object? obj) => Equals(obj as Money);

    public bool Equals(Money? other) => other is not null && Amount == other.Amount;

    public int CompareTo(Money? other) => other is null ? 1 : Amount.CompareTo(other.Amount);

    public override int GetHashCode() => Amount.GetHashCode();

    public override string ToString() => Amount.ToString("0.00");

    public static bool operator ==(Money left, Money right) => Equals(left, right);
    public static bool operator !=(Money left, Money right) => !Equals(left, right);
    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;
}
