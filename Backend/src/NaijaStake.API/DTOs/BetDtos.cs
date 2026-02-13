using NaijaStake.Domain.Entities;

namespace NaijaStake.API.Dtos;

public record CreateBetRequestDto(string Title, string Description, BetCategory Category, DateTime ClosingTime, DateTime ResolutionTime, IEnumerable<string> OutcomeOptions);
public record BetResponseDto(Guid Id, string Title, BetCategory Category, DateTime ClosingTime, decimal TotalStaked);
