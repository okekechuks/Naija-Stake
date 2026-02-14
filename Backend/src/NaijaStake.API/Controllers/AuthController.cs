using Microsoft.AspNetCore.Mvc;
using NaijaStake.API.Dtos;
using NaijaStake.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace NaijaStake.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthController(IUserService userService, ITokenService tokenService, IConfiguration config, IPasswordHasher passwordHasher, IRefreshTokenService refreshTokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
        _config = config;
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
        var jwt = _config.GetSection("JwtSettings");
        var expiryMinutes = int.TryParse(jwt["ExpiryMinutes"], out var m) ? m : 60;
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        // Create refresh token
        var refreshTtlMinutes = int.TryParse(jwt["RefreshExpiryMinutes"], out var r) ? r : 60 * 24 * 7;
        var refresh = await _refreshTokenService.CreateAsync(user.Id, TimeSpan.FromMinutes(refreshTtlMinutes));

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
        var jwt = _config.GetSection("JwtSettings");
        var expiryMinutes = int.TryParse(jwt["ExpiryMinutes"], out var m) ? m : 60;
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

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
