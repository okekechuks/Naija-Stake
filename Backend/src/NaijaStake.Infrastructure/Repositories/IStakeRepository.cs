using NaijaStake.Domain.Entities;

namespace NaijaStake.Infrastructure.Repositories;

/// <summary>
/// Stake repository interface for stake queries and operations.
/// </summary>
public interface IStakeRepository : IRepository<Stake, Guid>
{
    Task<IEnumerable<Stake>> GetByUserIdAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<Stake>> GetByBetIdAsync(Guid betId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Stake>> GetByOutcomeIdAsync(Guid outcomeId, CancellationToken cancellationToken = default);
    Task<Stake?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IEnumerable<Stake>> GetActiveStakesByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Stake>> GetByStatusAsync(StakeStatus status, CancellationToken cancellationToken = default);
}
