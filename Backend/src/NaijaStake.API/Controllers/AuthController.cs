using Microsoft.AspNetCore.Mvc;
using NaijaStake.API.Dtos;
using NaijaStake.Infrastructure.Configuration;
using NaijaStake.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;

namespace NaijaStake.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthController(IUserService userService, ITokenService tokenService, JwtSettings jwtSettings, IPasswordHasher passwordHasher, IRefreshTokenService refreshTokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings;
        _passwordHasher = passwordHasher;
        _refreshTokenService = refreshTokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto req)
    {
        var user = await _userService.GetByEmailAsync(req.Email);
        if (user == null) return Unauthorized();

        if (!_passwordHasher.Verify(user.PasswordHash, req.PasswordHash))
            return Unauthorized();

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.FirstName, user.LastName);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        // Create refresh token
        var refreshTtl = TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays);
        var refresh = await _refreshTokenService.CreateAsync(user.Id, refreshTtl);

        var resp = new LoginResponseDto(
            new AuthResponseDto(token, expiresAt),
            new RefreshDto(refresh.Token, refresh.ExpiresAt)
        );

        return Ok(resp);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken)) return BadRequest();

        var existing = await _refreshTokenService.GetByTokenAsync(req.RefreshToken);
        if (existing == null) return Unauthorized();
        if (existing.IsRevoked) return Unauthorized();
        if (existing.ExpiresAt <= DateTime.UtcNow) return Unauthorized();

        // Rotate: revoke old and issue new
        await _refreshTokenService.RevokeAsync(existing.Id);
        var newRefresh = await _refreshTokenService.CreateAsync(existing.UserId, existing.ExpiresAt - DateTime.UtcNow);

        // Generate new access token
        var user = await _userService.GetByIdAsync(existing.UserId);
        if (user == null) return Unauthorized();

        var access = _tokenService.GenerateToken(user.Id, user.Email, user.FirstName, user.LastName);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var resp = new RefreshResponseDto(access, expiresAt, newRefresh.Token, newRefresh.ExpiresAt);
        return Ok(resp);
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken)) return BadRequest();

        var existing = await _refreshTokenService.GetByTokenAsync(req.RefreshToken);
        if (existing == null) return NotFound();

        // Only allow owner to revoke
        var userIdClaim = User?.FindFirst("id")?.Value
                  ?? User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                  ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var uid) || uid != existing.UserId) return Forbid();

        await _refreshTokenService.RevokeAsync(existing.Id);
        return NoContent();
    }
}
