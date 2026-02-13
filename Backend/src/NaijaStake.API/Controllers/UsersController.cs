using Microsoft.AspNetCore.Mvc;
using NaijaStake.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using NaijaStake.API.Dtos;
using NaijaStake.Domain.Entities;

namespace NaijaStake.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto req)
    {
        var user = await _userService.CreateAsync(req.Email, req.PhoneNumber, req.PasswordHash, req.FirstName, req.LastName);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new UserResponseDto(user.Id, user.Email, user.FirstName, user.LastName));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(new UserResponseDto(user.Id, user.Email, user.FirstName, user.LastName));
    }
}
