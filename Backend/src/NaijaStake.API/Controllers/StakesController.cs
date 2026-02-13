using Microsoft.AspNetCore.Mvc;
using NaijaStake.Infrastructure.Services;
using NaijaStake.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using NaijaStake.API.Dtos;

namespace NaijaStake.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StakesController : ControllerBase
{
    private readonly IStakeService _stakeService;

    public StakesController(IStakeService stakeService)
    {
        _stakeService = stakeService;
    }

    public class PlaceStakeRequest
    {
        public Guid UserId { get; set; }
        public Guid BetId { get; set; }
        public Guid OutcomeId { get; set; }
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Place([FromBody] PlaceStakeRequestDto req)
    {
        var money = Money.From(req.Amount);
        var stake = await _stakeService.PlaceStakeAsync(req.UserId, req.BetId, req.OutcomeId, money, req.IdempotencyKey);
        return CreatedAtAction(nameof(Place), new { id = stake.Id }, new StakeResponseDto(stake.Id, stake.Status.ToString()));
    }
}
