using NaijaStake.Domain.Entities;

namespace NaijaStake.Infrastructure.Repositories;

/// <summary>
/// Bet repository interface for bet queries and operations.
/// </summary>
public interface IBetRepository : IRepository<Bet, Guid>
{
    Task<IEnumerable<Bet>> GetByCategoryAsync(BetCategory category, int limit = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bet>> GetOpenBetsAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bet>> GetClosedBetsAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<Bet?> GetWithOutcomesAsync(Guid betId, CancellationToken cancellationToken = default);
    Task<Bet?> GetWithStakesAsync(Guid betId, CancellationToken cancellationToken = default);
}
