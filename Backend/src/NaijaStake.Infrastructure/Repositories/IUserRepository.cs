using NaijaStake.Domain.Entities;

namespace NaijaStake.Infrastructure.Repositories;

/// <summary>
/// User repository interface for user-specific queries.
/// </summary>
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetWithWalletAsync(Guid userId, CancellationToken cancellationToken = default);
}
