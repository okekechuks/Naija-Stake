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

    public AuthController(IUserService userService, ITokenService tokenService, IConfiguration config)
    {
        _userService = userService;
        _tokenService = tokenService;
        _config = config;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto req)
    {
        var user = await _userService.GetByEmailAsync(req.Email);
        if (user == null) return Unauthorized();

        // For now expect caller to provide pre-hashed password; compare directly
        if (!string.Equals(user.PasswordHash, req.PasswordHash, StringComparison.Ordinal))
            return Unauthorized();

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.FirstName, user.LastName);

        var jwt = _config.GetSection("JwtSettings");
        var expiryMinutes = int.TryParse(jwt["ExpiryMinutes"], out var m) ? m : 60;
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        return Ok(new AuthResponseDto(token, expiresAt));
    }
}
