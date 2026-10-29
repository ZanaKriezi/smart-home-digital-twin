using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeTwin.Api.Contracts;
using SmartHomeTwin.Api.Data;
using SmartHomeTwin.Core.Simulation;

namespace SmartHomeTwin.Api.Controllers;

[ApiController]
[Route("api/readings")]
public class ReadingsController : ControllerBase
{
    private readonly TwinEngine _engine;
    private readonly TwinDbContext _db;

    public ReadingsController(TwinEngine engine, TwinDbContext db)
    {
        _engine = engine;
        _db = db;
    }

    ///Stored device readings of the current run for a simulated time range, optionally for one device.
    [HttpGet]
    public async Task<List<ReadingDto>> Get(string? deviceId, DateTime? from, DateTime? to)
    {
        if (_engine.CurrentRun == null)
            return new List<ReadingDto>();

        Guid runId = _engine.CurrentRun.Id;
        IQueryable<ReadingEntity> query = _db.Readings.Where(r => r.RunId == runId);
        if (!string.IsNullOrEmpty(deviceId))
            query = query.Where(r => r.DeviceId == deviceId);
        if (from != null)
            query = query.Where(r => r.Timestamp >= from);
        if (to != null)
            query = query.Where(r => r.Timestamp < to);

        return await query
            .OrderBy(r => r.Timestamp).ThenBy(r => r.DeviceId)
            .Select(r => new ReadingDto(r.Timestamp, r.DeviceId, r.PowerKw, r.EnergyKwh, r.IsOn))
            .ToListAsync();
    }
}
