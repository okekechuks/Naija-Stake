using NaijaStake.Domain.Entities;
using NaijaStake.Infrastructure.Repositories;

namespace NaijaStake.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> CreateAsync(string email, string phoneNumber, string passwordHash, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        var user = User.Create(email, phoneNumber, passwordHash, firstName, lastName);
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        return user;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _userRepository.GetByIdAsync(id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _userRepository.GetByEmailAsync(email, cancellationToken);
}
