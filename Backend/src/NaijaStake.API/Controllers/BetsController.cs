using Microsoft.AspNetCore.Mvc;
using NaijaStake.Infrastructure.Services;
using NaijaStake.API.Dtos;
using NaijaStake.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace NaijaStake.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BetsController : ControllerBase
{
    private readonly IBetService _betService;

    public BetsController(IBetService betService)
    {
        _betService = betService;
    }

    public class CreateBetRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public BetCategory Category { get; set; }
        public DateTime ClosingTime { get; set; }
        public DateTime ResolutionTime { get; set; }
        public IEnumerable<string> OutcomeOptions { get; set; } = Enumerable.Empty<string>();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateBetRequestDto req)
    {
        var bet = await _betService.CreateAsync(req.Title, req.Description, req.Category, req.ClosingTime, req.ResolutionTime, req.OutcomeOptions);
        return CreatedAtAction(nameof(GetOpen), new { id = bet.Id }, new BetResponseDto(bet.Id, bet.Title, bet.Category, bet.ClosingTime, bet.TotalStaked.Amount));
    }

    [HttpGet("open")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOpen()
    {
        var bets = await _betService.GetOpenBetsAsync();
        var dto = bets.Select(b => new BetResponseDto(b.Id, b.Title, b.Category, b.ClosingTime, b.TotalStaked.Amount));
        return Ok(dto);
    }
}
