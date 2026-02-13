namespace NaijaStake.API.Dtos;

public record RegisterRequestDto(string Email, string PhoneNumber, string PasswordHash, string FirstName, string LastName);
public record LoginRequestDto(string Email, string PasswordHash);
public record UserResponseDto(Guid Id, string Email, string FirstName, string LastName);
