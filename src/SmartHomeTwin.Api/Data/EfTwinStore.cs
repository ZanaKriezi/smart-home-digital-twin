using Microsoft.EntityFrameworkCore;
using SmartHomeTwin.Core.Abstractions;
using SmartHomeTwin.Core.Simulation;

namespace SmartHomeTwin.Api.Data;

///Saves the twin's history to the database through EF Core.
public class EfTwinStore : ITwinStore
{
    private readonly IDbContextFactory<TwinDbContext> _dbFactory;

    public EfTwinStore(IDbContextFactory<TwinDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public void BeginRun(SimulationRun run)
    {
        using TwinDbContext db = _dbFactory.CreateDbContext();
        db.Runs.Add(new SimulationRunEntity
        {
            Id = run.Id,
            ScenarioId = run.ScenarioId,
            Seed = run.Seed,
            SimulationStart = run.SimulationStart,
            StartedAtUtc = run.StartedAtUtc
        });
        db.SaveChanges();
    }

    
    /// Writes the readings, the home snapshot and the new alerts of one step with a single SaveChanges call.
    /// EF Core runs it in one transaction, so the step is saved completely or not at all.
    
    public void SaveTick(TickResult tick)
    {
        using TwinDbContext db = _dbFactory.CreateDbContext();
        double hours = tick.StepLength.TotalHours;
        var b = tick.Balance;
        var s = tick.Snapshot;

        db.Readings.AddRange(tick.Readings.Select(r => new ReadingEntity
        {
            RunId = tick.Run.Id,
            DeviceId = r.DeviceId,
            Timestamp = tick.Timestamp,
            PowerKw = r.AveragePowerKw,
            EnergyKwh = r.AveragePowerKw * hours,
            IsOn = r.IsOn
        }));

        db.HomeSnapshots.Add(new HomeSnapshotEntity
        {
            RunId = tick.Run.Id,
            TickIndex = tick.TickIndex,
            Timestamp = tick.Timestamp,
            LoadKw = b.LoadKw,
            PvKw = b.PvKw,
            BatteryChargeKw = b.BatteryChargeKw,
            BatteryDischargeKw = b.BatteryDischargeKw,
            BatterySocKwh = s.BatterySocKwh,
            BatterySocPercent = s.BatterySocPercent,
            GridImportKw = b.GridImportKw,
            GridExportKw = b.GridExportKw
        });

        db.Alerts.AddRange(tick.NewAlerts.Select(a => new AlertEntity
        {
            RunId = tick.Run.Id,
            Timestamp = a.Timestamp,
            Type = a.Type.ToString(),
            Message = a.Message,
            Value = a.Value,
            Threshold = a.Threshold
        }));

        db.SaveChanges();
    }
}
