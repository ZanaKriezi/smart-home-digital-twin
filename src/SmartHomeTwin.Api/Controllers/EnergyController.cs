using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeTwin.Api.Contracts;
using SmartHomeTwin.Api.Data;
using SmartHomeTwin.Core.Energy;
using SmartHomeTwin.Core.Simulation;

namespace SmartHomeTwin.Api.Controllers;

[ApiController]
[Route("api/energy")]
public class EnergyController : ControllerBase
{
    private readonly TwinEngine _engine;
    private readonly TwinDbContext _db;

    public EnergyController(TwinEngine engine, TwinDbContext db)
    {
        _engine = engine;
        _db = db;
    }

    ///Current state of the whole home: power flows, battery, devices and active alerts.
    [HttpGet("current")]
    public ActionResult<HomeSnapshot> Current()
    {
        if (_engine.LatestSnapshot == null)
            return NotFound("No scenario has been started yet.");
        return _engine.LatestSnapshot;
    }

    /// Energy balance per step for a simulated time range (default: the whole current run).
    [HttpGet("history")]
    public async Task<List<EnergyPointDto>> History(DateTime? from, DateTime? to)
    {
        if (_engine.CurrentRun == null)
            return new List<EnergyPointDto>();

        Guid runId = _engine.CurrentRun.Id;
        IQueryable<HomeSnapshotEntity> query = _db.HomeSnapshots.Where(s => s.RunId == runId);
        if (from != null)
            query = query.Where(s => s.Timestamp >= from);
        if (to != null)
            query = query.Where(s => s.Timestamp < to);

        return await query
            .OrderBy(s => s.Timestamp)
            .Select(s => new EnergyPointDto(s.Timestamp, s.LoadKw, s.PvKw, s.BatteryChargeKw, s.BatteryDischargeKw,
                s.BatterySocPercent, s.GridImportKw, s.GridExportKw))
            .ToListAsync();
    }

    
    /// Indicators for the current run, or for a simulated time range when from/to are given:
    /// energy per appliance, self-consumption rate, self-sufficiency rate, imports and exports.
    [HttpGet("kpis")]
    public async Task<KpiSummary> Kpis(DateTime? from, DateTime? to)
    {
        // Without a time range: the totals the engine keeps for the whole run.
        if (from == null && to == null)
            return _engine.GetRunKpis();

        var kpis = new KpiAccumulator();
        if (_engine.CurrentRun == null)
            return kpis.GetSummary();

        Guid runId = _engine.CurrentRun.Id;
        IQueryable<HomeSnapshotEntity> snapshots = _db.HomeSnapshots.Where(s => s.RunId == runId);
        IQueryable<ReadingEntity> readings = _db.Readings.Where(r => r.RunId == runId);
        if (from != null)
        {
            snapshots = snapshots.Where(s => s.Timestamp >= from);
            readings = readings.Where(r => r.Timestamp >= from);
        }
        if (to != null)
        {
            snapshots = snapshots.Where(s => s.Timestamp < to);
            readings = readings.Where(r => r.Timestamp < to);
        }

        // Add up the stored steps of the range.
        foreach (HomeSnapshotEntity s in await snapshots.OrderBy(s => s.Timestamp).ToListAsync())
        {
            var balance = new EnergyBalanceResult(s.LoadKw, s.PvKw, Math.Min(s.LoadKw, s.PvKw), s.BatteryChargeKw,
                s.BatteryDischargeKw, s.GridImportKw, s.GridExportKw, s.BatterySocKwh, s.BatterySocKwh);
            kpis.Add(balance, _engine.Clock.StepLength);
        }

        // Energy per appliance in the range.
        List<string> applianceIds = _engine.Home.Consumers.Select(d => d.Id).ToList();
        Dictionary<string, double> applianceKwh = await readings
            .Where(r => applianceIds.Contains(r.DeviceId))
            .GroupBy(r => r.DeviceId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(r => r.EnergyKwh));
        kpis.AddApplianceEnergy(applianceKwh);

        return kpis.GetSummary();
    }
}
