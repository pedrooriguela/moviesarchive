using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesArchive.Api.Auth;
using MoviesArchive.Domain.Interfaces;

namespace MoviesArchive.Api.Controllers;

// Painel do administrador: estatísticas de uso da plataforma
[ApiController]
[Authorize(Policy = AdminPolicy.Name)]
public class AdminStatsController : Controller
{
    private const int MaxPeriodDays = 365;

    private readonly IPlatformStatsService _platformStatsService;

    public AdminStatsController(
        IPlatformStatsService platformStatsService)
    {
        _platformStatsService = platformStatsService;
    }

    // days: últimos N dias (1 a 365), contando hoje. Sem days, desde o início (o gráfico vira por mês).
    [HttpGet("api/admin/stats")]
    public async Task<IActionResult> GetAsync([FromQuery] int? days)
    {
        if (days is < 1 or > MaxPeriodDays)
            return BadRequest(new { field = "days", message = $"O período deve ficar entre 1 e {MaxPeriodDays} dias." });

        return Ok(await _platformStatsService.GetAsync(days));
    }
}
