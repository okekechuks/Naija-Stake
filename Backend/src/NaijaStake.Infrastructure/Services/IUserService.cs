using NaijaStake.Domain.Entities;

namespace NaijaStake.Infrastructure.Services;

public interface IUserService
{
    Task<User> CreateAsync(string email, string phoneNumber, string passwordHash, string firstName, string lastName, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
