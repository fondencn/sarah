using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.Statistics.WebApi.Services;

namespace Sarah.Statistics.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/statistics/energy")]
public class EnergyStatisticsController : ControllerBase
{
    private static readonly TimeSpan MaxRange = TimeSpan.FromDays(400);
    private readonly EnergyQueryService _query;

    public EnergyStatisticsController(EnergyQueryService query)
    {
        _query = query;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<EnergySummaryDto>> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        if (!TryGetRange(from, to, out var f, out var t, out var error)) return BadRequest(error);
        return Ok(await _query.GetSummaryAsync(f, t, DateTime.UtcNow, ct));
    }

    [HttpGet("devices")]
    public async Task<ActionResult<List<EnergyDeviceStatisticsDto>>> GetDevices([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] EnergyGranularity? granularity, CancellationToken ct)
    {
        if (!TryGetRange(from, to, out var f, out var t, out var error)) return BadRequest(error);
        return Ok(await _query.GetDevicesAsync(f, t, EnergyQueryService.ResolveGranularity(f, t, granularity), DateTime.UtcNow, ct));
    }

    [HttpGet("devices/{deviceId:int}/timeseries")]
    public async Task<ActionResult<List<EnergyTimeseriesPointDto>>> GetTimeseries(int deviceId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] EnergyGranularity? granularity, CancellationToken ct)
    {
        if (!TryGetRange(from, to, out var f, out var t, out var error)) return BadRequest(error);
        return Ok(await _query.GetTimeseriesAsync(deviceId, f, t, EnergyQueryService.ResolveGranularity(f, t, granularity), ct));
    }

    [HttpGet("live")]
    public async Task<ActionResult<List<EnergyLiveDeviceDto>>> GetLive(CancellationToken ct)
    {
        return Ok(await _query.GetLiveAsync(DateTime.UtcNow, ct));
    }

    private static bool TryGetRange(DateTime? from, DateTime? to, out DateTime f, out DateTime t, out string error)
    {
        t = (to ?? DateTime.UtcNow).ToUniversalTime();
        f = (from ?? t.AddDays(-1)).ToUniversalTime();
        error = string.Empty;
        if (f >= t) { error = "'from' must be before 'to'."; return false; }
        if (t - f > MaxRange) { error = "The requested range is too large."; return false; }
        return true;
    }
}
