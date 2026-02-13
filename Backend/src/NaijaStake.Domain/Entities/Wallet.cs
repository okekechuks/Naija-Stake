using NaijaStake.Domain.ValueObjects;

namespace NaijaStake.Domain.Entities;

/// <summary>
/// Wallet aggregate root. Represents a user's wallet with balance tracking.
/// The actual source of truth for balances is calculated from the immutable transaction ledger,
/// but we cache it here for query performance.
/// 
/// CRITICAL RULE: All money operations must be accompanied by a Transaction record.
/// Never modify balance without creating a corresponding transaction.
/// </summary>
public class Wallet
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User? User { get; private set; }
    
    /// <summary>
    /// Available balance (not locked in active stakes).
    /// Can only be positive. Calculated from transaction ledger in production flows.
    /// </summary>
    public Money AvailableBalance { get; private set; }

    /// <summary>
    /// Locked funds in active stakes. Cannot be withdrawn.
    /// Sum of all StakeLocked transactions minus StakeRefund transactions.
    /// </summary>
    public Money LockedBalance { get; private set; }

    /// <summary>
    /// Total balance = AvailableBalance + LockedBalance
    /// </summary>
    public Money TotalBalance =>
        AvailableBalance.Add(LockedBalance);

    // Audit
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Immutable transaction ledger - source of truth for all balance changes
    public ICollection<Transaction> Transactions { get; private set; } = new List<Transaction>();

    private Wallet() 
    { 
        AvailableBalance = Money.Zero;
        LockedBalance = Money.Zero;
    }

    /// <summary>
    /// Factory method to create a new wallet for a user.
    /// </summary>
    public static Wallet Create(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        return new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AvailableBalance = Money.Zero,
            LockedBalance = Money.Zero,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Records a deposit transaction. Increases available balance.
    /// This should be called AFTER the transaction is persisted to the database.
    /// </summary>
    public void RecordDeposit(Money amount, string transactionId)
    {
        if (amount == null)
            throw new ArgumentNullException(nameof(amount));

        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("Transaction ID is required.", nameof(transactionId));

        AvailableBalance = AvailableBalance.Add(amount);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a stake being locked. Moves funds from available to locked.
    /// This happens when a user places a stake on an active bet.
    /// </summary>
    public void RecordStakeLocked(Money amount, string transactionId)
    {
        if (amount == null)
            throw new ArgumentNullException(nameof(amount));

        if (!AvailableBalance.IsGreaterThanOrEqualTo(amount))
            throw new InvalidOperationException("Insufficient available balance to lock stake.");

        AvailableBalance = AvailableBalance.Subtract(amount);
        LockedBalance = LockedBalance.Add(amount);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a stake refund. Moves funds from locked back to available.
    /// This happens when a bet is cancelled or user loses a stake.
    /// </summary>
    public void RecordStakeRefund(Money amount, string transactionId)
    {
        if (amount == null)
            throw new ArgumentNullException(nameof(amount));

        if (!LockedBalance.IsGreaterThanOrEqualTo(amount))
            throw new InvalidOperationException("Insufficient locked balance to refund.");

        LockedBalance = LockedBalance.Subtract(amount);
        AvailableBalance = AvailableBalance.Add(amount);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a winning payout. This moves funds from locked to available PLUS winnings.
    /// The transaction record contains the actual payout amount (stake + winnings).
    /// </summary>
    public void RecordWinPayout(Money payoutAmount, string transactionId)
    {
        if (payoutAmount == null)
            throw new ArgumentNullException(nameof(payoutAmount));

        // Payout amount already includes the original stake being removed from locked.
        // Release the corresponding locked funds (up to the locked balance) and
        // add the full payout to available balance.
        if (LockedBalance.IsLessThanOrEqualTo(payoutAmount))
        {
            // Payout covers all locked funds
            LockedBalance = Money.Zero;
        }
        else
        {
            // Payout is smaller than locked balance (edge-case) - subtract payout portion
            LockedBalance = LockedBalance.Subtract(payoutAmount);
        }

        AvailableBalance = AvailableBalance.Add(payoutAmount);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a platform fee deduction from available balance.
    /// </summary>
    public void RecordPlatformFee(Money feeAmount, string transactionId)
    {
        if (feeAmount == null)
            throw new ArgumentNullException(nameof(feeAmount));

        if (!AvailableBalance.IsGreaterThanOrEqualTo(feeAmount))
            throw new InvalidOperationException("Insufficient available balance to deduct fee.");

        AvailableBalance = AvailableBalance.Subtract(feeAmount);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns true if the user can afford to lock the given amount.
    /// </summary>
    public bool CanAfford(Money amount)
    {
        return AvailableBalance.IsGreaterThanOrEqualTo(amount);
    }
}
