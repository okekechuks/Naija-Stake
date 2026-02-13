using NaijaStake.Domain.ValueObjects;

namespace NaijaStake.Domain.Entities;

/// <summary>
/// Immutable transaction record. This is the source of truth for all wallet changes.
/// Every transaction is append-only. Never update or delete transactions.
/// This follows an immutable ledger pattern for maximum auditability.
/// </summary>
public class Transaction
{
    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public Guid UserId { get; private set; }
    
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; } = Money.Zero;
    
    // Descriptive information
    public string Description { get; private set; } = string.Empty;
    
    // Reference to related entities (optional, for auditing)
    public Guid? BetId { get; private set; }
    public Guid? StakeId { get; private set; }
    public Guid? PaymentId { get; private set; }
    
    // Status tracking
    public TransactionStatus Status { get; private set; } = TransactionStatus.Completed;
    public DateTime CreatedAt { get; private set; }
    
    // Idempotency: External reference to prevent duplicate processing
    public string? IdempotencyKey { get; private set; }

    // Audit metadata
    public string? Metadata { get; private set; } // JSON metadata for additional context

    private Transaction() { }

    /// <summary>
    /// Factory method for creating a transaction.
    /// Transactions are immutable once created.
    /// </summary>
    public static Transaction Create(
        Guid walletId,
        Guid userId,
        TransactionType type,
        Money amount,
        string description,
        Guid? betId = null,
        Guid? stakeId = null,
        Guid? paymentId = null,
        string? idempotencyKey = null,
        string? metadata = null)
    {
        if (walletId == Guid.Empty)
            throw new ArgumentException("Wallet ID is required.", nameof(walletId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (amount == null)
            throw new ArgumentNullException(nameof(amount));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        return new Transaction
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            UserId = userId,
            Type = type,
            Amount = amount,
            Description = description,
            BetId = betId,
            StakeId = stakeId,
            PaymentId = paymentId,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            IdempotencyKey = idempotencyKey,
            Metadata = metadata
        };
    }

    public override string ToString() => 
        $"Transaction: {Type} - {Amount} on {CreatedAt:yyyy-MM-dd HH:mm:ss}";
}

public enum TransactionType
{
    /// <summary>User deposits money into their wallet</summary>
    Deposit = 1,

    /// <summary>Funds locked from available to locked when placing a stake</summary>
    StakeLocked = 2,

    /// <summary>Locked funds returned to available (bet cancelled, stake lost, or refund)</summary>
    StakeRefund = 3,

    /// <summary>Winnings paid out (original stake + winnings moved to available)</summary>
    WinPayout = 4,

    /// <summary>Platform fee deducted from available balance</summary>
    PlatformFee = 5,

    /// <summary>Withdrawal request initiated (money leaves the platform)</summary>
    Withdrawal = 6
}

public enum TransactionStatus
{
    /// <summary>Transaction is completed and final</summary>
    Completed = 1,

    /// <summary>Transaction is pending processing (rarely used in this system)</summary>
    Pending = 2,

    /// <summary>Transaction failed and was rolled back</summary>
    Failed = 3
}
