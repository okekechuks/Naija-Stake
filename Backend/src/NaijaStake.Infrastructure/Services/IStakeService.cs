using NaijaStake.Domain.Entities;
using NaijaStake.Domain.ValueObjects;

namespace NaijaStake.Infrastructure.Services;

public interface IStakeService
{
    Task<Stake> PlaceStakeAsync(Guid userId, Guid betId, Guid outcomeId, Money stakeAmount, string idempotencyKey, CancellationToken cancellationToken = default);
}
