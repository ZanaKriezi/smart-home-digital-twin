using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeTwin.Api.Contracts;
using SmartHomeTwin.Api.Data;
using SmartHomeTwin.Core.Simulation;

namespace SmartHomeTwin.Api.Controllers;

[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly TwinEngine _engine;
    private readonly TwinDbContext _db;

    public AlertsController(TwinEngine engine, TwinDbContext db)
    {
        _engine = engine;
        _db = db;
    }

    /// The newest alerts of the current run.
    [HttpGet]
    public async Task<List<AlertDto>> Get(int limit = 50)
    {
        if (_engine.CurrentRun == null)
            return new List<AlertDto>();

        Guid runId = _engine.CurrentRun.Id;
        return await _db.Alerts
            .Where(a => a.RunId == runId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .Select(a => new AlertDto(a.Timestamp, a.Type, a.Message, a.Value, a.Threshold))
            .ToListAsync();
    }
}
