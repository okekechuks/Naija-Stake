using NaijaStake.Domain.Entities;

namespace NaijaStake.Infrastructure.Services;

public interface IBetService
{
    Task<Bet> CreateAsync(string title, string description, BetCategory category, DateTime closingTime, DateTime resolutionTime, IEnumerable<string> outcomeOptions, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bet>> GetOpenBetsAsync(int limit = 50, CancellationToken cancellationToken = default);
}
