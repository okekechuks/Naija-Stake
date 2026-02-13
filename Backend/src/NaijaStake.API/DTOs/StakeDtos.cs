namespace NaijaStake.API.Dtos;

public record PlaceStakeRequestDto(Guid UserId, Guid BetId, Guid OutcomeId, decimal Amount, string IdempotencyKey);
public record StakeResponseDto(Guid Id, string Status);
