using Microsoft.AspNetCore.Mvc;
using NaijaStake.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using NaijaStake.API.Dtos;
using NaijaStake.Domain.Entities;

namespace NaijaStake.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletsController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public class CreateWalletRequest { public Guid UserId { get; set; } }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateWalletRequestDto req)
    {
        var wallet = await _walletService.CreateAsync(req.UserId);
        return CreatedAtAction(nameof(GetByUserId), new { userId = wallet.UserId }, new WalletResponseDto(wallet.Id, wallet.UserId, wallet.AvailableBalance.Amount, wallet.LockedBalance.Amount));
    }

    [HttpGet("user/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetByUserId(Guid userId)
    {
        var wallet = await _walletService.GetByUserIdAsync(userId);
        if (wallet == null) return NotFound();
        return Ok(new WalletResponseDto(wallet.Id, wallet.UserId, wallet.AvailableBalance.Amount, wallet.LockedBalance.Amount));
    }
}
