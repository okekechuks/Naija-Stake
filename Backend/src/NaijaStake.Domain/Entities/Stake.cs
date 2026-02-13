using NaijaStake.Domain.ValueObjects;

namespace NaijaStake.Domain.Entities;

/// <summary>
/// Stake entity represents a user's stake on a specific bet outcome.
/// This is immutable once created and reflects the state of a user's participation.
/// </summary>
public class Stake
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid BetId { get; private set; }
    public Guid OutcomeId { get; private set; }
    
    public Money StakeAmount { get; private set; } = Money.Zero;
    
    // Status tracking
    public StakeStatus Status { get; private set; } = StakeStatus.Active;
    
    // Resolution
    public Money? PotentialPayout { get; private set; }
    public Money? ActualPayout { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    
    // Audit
    public DateTime CreatedAt { get; private set; }
    
    // Idempotency key to prevent duplicate stake placement
    public string IdempotencyKey { get; private set; } = string.Empty;

    // Navigation
    public User? User { get; private set; }
    public Bet? Bet { get; private set; }
    public Outcome? Outcome { get; private set; }

    private Stake() { }

    /// <summary>
    /// Factory method to create a new stake.
    /// This should only be called by the staking service after validations.
    /// </summary>
    public static Stake Create(
        Guid userId,
        Guid betId,
        Guid outcomeId,
        Money stakeAmount,
        string idempotencyKey)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (betId == Guid.Empty)
            throw new ArgumentException("Bet ID is required.", nameof(betId));

        if (outcomeId == Guid.Empty)
            throw new ArgumentException("Outcome ID is required.", nameof(outcomeId));

        if (stakeAmount == null)
            throw new ArgumentNullException(nameof(stakeAmount));

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        return new Stake
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BetId = betId,
            OutcomeId = outcomeId,
            StakeAmount = stakeAmount,
            Status = StakeStatus.Active,
            PotentialPayout = null,
            ActualPayout = null,
            CreatedAt = DateTime.UtcNow,
            IdempotencyKey = idempotencyKey
        };
    }

    /// <summary>
    /// Marks this stake as won with the calculated payout.
    /// This is idempotent - calling multiple times is safe.
    /// </summary>
    public void MarkAsWon(Money payout)
    {
        if (payout == null)
            throw new ArgumentNullException(nameof(payout));

        if (Status == StakeStatus.Won)
            return; // Already marked as won, idempotent

        if (Status != StakeStatus.Active)
            throw new InvalidOperationException($"Only active stakes can be marked as won. Current status: {Status}");

        Status = StakeStatus.Won;
        ActualPayout = payout;
        ResolvedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this stake as lost (refund only, no payout).
    /// This is idempotent - calling multiple times is safe.
    /// </summary>
    public void MarkAsLost()
    {
        if (Status == StakeStatus.Lost)
            return; // Already marked as lost, idempotent

        if (Status != StakeStatus.Active)
            throw new InvalidOperationException($"Only active stakes can be marked as lost. Current status: {Status}");

        Status = StakeStatus.Lost;
        ResolvedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the stake if the bet is cancelled.
    /// Stake amount will be refunded to the user.
    /// </summary>
    public void Cancel()
    {
        if (Status == StakeStatus.Cancelled)
            return; // Already cancelled, idempotent

        if (Status != StakeStatus.Active)
            throw new InvalidOperationException($"Only active stakes can be cancelled. Current status: {Status}");

        Status = StakeStatus.Cancelled;
        ResolvedAt = DateTime.UtcNow;
    }
}

public enum StakeStatus
{
    /// <summary>Stake is active and waiting for bet resolution</summary>
    Active = 1,

    /// <summary>Stake won and payout was issued</summary>
    Won = 2,

    /// <summary>Stake lost, only refund was issued</summary>
    Lost = 3,

    /// <summary>Stake was cancelled (bet cancelled), refund issued</summary>
    Cancelled = 4
}
