using NaijaStake.Domain.Entities;
using NaijaStake.Infrastructure.Repositories;

namespace NaijaStake.Infrastructure.Services;

public class BetService : IBetService
{
    private readonly IBetRepository _betRepository;

    public BetService(IBetRepository betRepository)
    {
        _betRepository = betRepository;
    }

    public async Task<Bet> CreateAsync(string title, string description, BetCategory category, DateTime closingTime, DateTime resolutionTime, IEnumerable<string> outcomeOptions, CancellationToken cancellationToken = default)
    {
        var bet = Bet.Create(title, description, category, closingTime, resolutionTime, outcomeOptions);
        await _betRepository.AddAsync(bet, cancellationToken);
        await _betRepository.SaveChangesAsync(cancellationToken);
        return bet;
    }

    public Task<IEnumerable<Bet>> GetOpenBetsAsync(int limit = 50, CancellationToken cancellationToken = default)
        => _betRepository.GetOpenBetsAsync(limit, cancellationToken);
}
