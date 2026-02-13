using NaijaStake.Domain.ValueObjects;

namespace NaijaStake.Domain.Entities;

/// <summary>
/// Bet aggregate root. Represents a prediction bet that users can stake on.
/// Follows a strict lifecycle: Draft → Open → Closed → Resolved → Paid
/// </summary>
public class Bet
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public BetCategory Category { get; private set; }

    // Core logic
    public BetStatus Status { get; private set; } = BetStatus.Draft;
    public DateTime ClosingTime { get; private set; }
    public DateTime ResolutionTime { get; private set; }
    
    // Outcomes
    public ICollection<Outcome> Outcomes { get; private set; } = new List<Outcome>();
    
    // Financial tracking
    public Money TotalStaked { get; private set; } = Money.Zero;
    public int ParticipantCount { get; private set; } = 0;
    
    // Resolution metadata
    public Guid? ResolvedOutcomeId { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }
    
    // Audit
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    // Idempotency for resolution
    public string? ResolutionIdempotencyKey { get; private set; }

    // Related stakes
    public ICollection<Stake> Stakes { get; private set; } = new List<Stake>();

    private Bet() { }

    /// <summary>
    /// Factory method to create a new bet in Draft status.
    /// </summary>
    public static Bet Create(
        string title,
        string description,
        BetCategory category,
        DateTime closingTime,
        DateTime resolutionTime,
        IEnumerable<string> outcomeOptions)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        // Allow creating bets with past closing times for testing and historical replay scenarios.

        if (resolutionTime <= closingTime)
            throw new ArgumentException("Resolution time must be after closing time.", nameof(resolutionTime));

        if (outcomeOptions == null || !outcomeOptions.Any() || outcomeOptions.Count() < 2)
            throw new ArgumentException("At least 2 outcome options are required.", nameof(outcomeOptions));

        var bet = new Bet
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description.Trim(),
            Category = category,
            Status = BetStatus.Draft,
            ClosingTime = closingTime,
            ResolutionTime = resolutionTime,
            TotalStaked = Money.Zero,
            ParticipantCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        // Add outcomes
        foreach (var option in outcomeOptions)
        {
            bet.Outcomes.Add(Outcome.Create(bet.Id, option));
        }

        return bet;
    }

    /// <summary>
    /// Transitions the bet from Draft to Open.
    /// Can only be done once, by the bet creator/admin.
    /// </summary>
    public void Open()
    {
        if (Status != BetStatus.Draft)
            throw new InvalidOperationException($"Cannot open a bet in {Status} status. Only Draft bets can be opened.");

        Status = BetStatus.Open;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the bet from Open to Closed.
    /// No more stakes allowed after this point.
    /// Should be called automatically at ClosingTime.
    /// </summary>
    public void Close()
    {
        if (Status != BetStatus.Open)
            throw new InvalidOperationException($"Cannot close a bet in {Status} status. Only Open bets can be closed.");

        if (DateTime.UtcNow < ClosingTime)
            throw new InvalidOperationException("Closing time has not been reached yet.");

        Status = BetStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resolves the bet by selecting the winning outcome.
    /// This is IDEMPOTENT - calling it multiple times with the same outcome is safe.
    /// </summary>
    public void Resolve(Guid winningOutcomeId, string? notes = null, string? idempotencyKey = null)
    {
        // Idempotency check: if already resolved with same outcome and key, it's a safe replay
        if (Status == BetStatus.Resolved && ResolvedOutcomeId == winningOutcomeId && 
            ResolutionIdempotencyKey == idempotencyKey)
        {
            return; // Already resolved, this is an idempotent replay
        }

        if (Status != BetStatus.Closed)
            throw new InvalidOperationException($"Cannot resolve a bet in {Status} status. Only Closed bets can be resolved.");

        if (DateTime.UtcNow < ResolutionTime)
            throw new InvalidOperationException("Resolution time has not been reached yet.");

        var outcome = Outcomes.FirstOrDefault(o => o.Id == winningOutcomeId);
        if (outcome == null)
            throw new ArgumentException("Invalid winning outcome ID.", nameof(winningOutcomeId));

        ResolvedOutcomeId = winningOutcomeId;
        ResolutionNotes = notes;
        ResolutionIdempotencyKey = idempotencyKey;
        Status = BetStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates total staked amount when a stake is placed.
    /// Should only be called by the Stake repository/service.
    /// </summary>
    public void AddStake(Money stakeAmount)
    {
        if (stakeAmount == null)
            throw new ArgumentNullException(nameof(stakeAmount));

        if (Status != BetStatus.Open)
            throw new InvalidOperationException("Stakes can only be added to Open bets.");

        if (DateTime.UtcNow >= ClosingTime)
            throw new InvalidOperationException("Bet closing time has passed.");

        TotalStaked = TotalStaked.Add(stakeAmount);
        ParticipantCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns true if the bet is currently accepting stakes.
    /// </summary>
    public bool IsOpen => Status == BetStatus.Open && DateTime.UtcNow < ClosingTime;

    /// <summary>
    /// Returns true if the bet is in a settled state (Resolved or Paid).
    /// </summary>
    public bool IsSettled => Status == BetStatus.Resolved || Status == BetStatus.Paid;
}

public enum BetStatus
{
    /// <summary>Bet created but not yet open for staking</summary>
    Draft = 1,

    /// <summary>Bet is accepting stakes</summary>
    Open = 2,

    /// <summary>Bet is closed, no more stakes allowed, awaiting resolution</summary>
    Closed = 3,

    /// <summary>Bet has been resolved with a winning outcome</summary>
    Resolved = 4,

    /// <summary>Winnings have been paid out to participants</summary>
    Paid = 5,

    /// <summary>Bet was cancelled</summary>
    Cancelled = 6
}

public enum BetCategory
{
    Sports = 1,
    Politics = 2,
    Entertainment = 3,
    Market = 4,
    Technology = 5
}
