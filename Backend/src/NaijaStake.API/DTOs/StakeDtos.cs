namespace NaijaStake.API.DTOs;

/// <summary>
/// DTO for placing a stake (user request).
/// </summary>
public class PlaceStakeDto
{
    public Guid BetId { get; set; }
    public Guid OutcomeId { get; set; }
    public decimal Amount { get; set; }
    public string IdempotencyKey { get; set; } = null!;
}

/// <summary>
/// DTO for stake response.
/// </summary>
public class StakeDto
{
    public Guid Id { get; set; }
    public Guid BetId { get; set; }
    public Guid OutcomeId { get; set; }
    public decimal StakeAmount { get; set; }
    public string Status { get; set; } = null!;
    public decimal? ActualPayout { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>
/// DTO for user's stakes response.
/// </summary>
public class UserStakesDto
{
    public List<StakeDto> Stakes { get; set; } = new();
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
}

/// <summary>
/// DTO for stake placement response.
/// </summary>
public class StakePlacedDto
{
    public Guid StakeId { get; set; }
    public decimal RemainingBalance { get; set; }
    public decimal LockedBalance { get; set; }
    public DateTime CreatedAt { get; set; }
}
