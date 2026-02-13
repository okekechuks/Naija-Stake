using NaijaStake.Domain.Entities;
using NaijaStake.Domain.ValueObjects;
using NaijaStake.Infrastructure.Repositories;

namespace NaijaStake.Infrastructure.Services;

public class StakeService : IStakeService
{
    private readonly IStakeRepository _stakeRepository;
    private readonly IBetRepository _betRepository;
    private readonly IUserRepository _userRepository;

    public StakeService(IStakeRepository stakeRepository, IBetRepository betRepository, IUserRepository userRepository)
    {
        _stakeRepository = stakeRepository;
        _betRepository = betRepository;
        _userRepository = userRepository;
    }

    public async Task<Stake> PlaceStakeAsync(Guid userId, Guid betId, Guid outcomeId, Money stakeAmount, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new ArgumentException("User not found", nameof(userId));

        var bet = await _betRepository.GetByIdAsync(betId, cancellationToken);
        if (bet == null)
            throw new ArgumentException("Bet not found", nameof(betId));

        var stake = Stake.Create(userId, betId, outcomeId, stakeAmount, idempotencyKey);
        await _stakeRepository.AddAsync(stake, cancellationToken);
        await _stakeRepository.SaveChangesAsync(cancellationToken);
        return stake;
    }
}
