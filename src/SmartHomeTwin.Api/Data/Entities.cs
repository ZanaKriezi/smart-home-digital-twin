namespace SmartHomeTwin.Api.Data;

/// A modelled device (reference data, written at startup from appsettings.json).
public class DeviceEntity
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Category { get; set; } = "";
    public double RatedPowerKw { get; set; }
}

///A predefined scenario (reference data, written at startup from scenarios.json).
public class ScenarioEntity
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Seed { get; set; }
    public DateOnly StartDate { get; set; }
}

///One run of a scenario. All time series below belong to a run.
public class SimulationRunEntity
{
    public Guid Id { get; set; }
    public string ScenarioId { get; set; } = "";
    public int Seed { get; set; }
    public DateTime SimulationStart { get; set; }
    public DateTime StartedAtUtc { get; set; }
}

///One device reading for one 15-minute step.
public class ReadingEntity
{
    public long Id { get; set; }
    public Guid RunId { get; set; }
    public string DeviceId { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public double PowerKw { get; set; }
    public double EnergyKwh { get; set; }
    public bool IsOn { get; set; }
}

/// The energy balance of the whole home for one step.
public class HomeSnapshotEntity
{
    public long Id { get; set; }
    public Guid RunId { get; set; }
    public long TickIndex { get; set; }
    public DateTime Timestamp { get; set; }
    public double LoadKw { get; set; }
    public double PvKw { get; set; }
    public double BatteryChargeKw { get; set; }
    public double BatteryDischargeKw { get; set; }
    public double BatterySocKwh { get; set; }
    public double BatterySocPercent { get; set; }
    public double GridImportKw { get; set; }
    public double GridExportKw { get; set; }
}

public class AlertEntity
{
    public long Id { get; set; }
    public Guid RunId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public double Value { get; set; }
    public double Threshold { get; set; }
}
