namespace SmartHomeTwin.Core.Scenarios;

/// <summary>
/// A scenario is a fixed set of parameters for the simulator. The twin core runs unchanged in every scenario.
/// Scenarios are loaded from scenarios.json.
/// </summary>
public record ScenarioDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>Random seed. The same scenario with the same seed always gives the same results.</summary>
    public int Seed { get; set; } = 42;

    /// <summary>Simulated date the run starts on, at 00:00.</summary>
    public DateOnly StartDate { get; set; }

    public double InitialBatterySocPercent { get; set; } = 50;

    public SolarSettings Solar { get; set; } = new();

    /// <summary>Planned appliance uses for every simulated day.</summary>
    public List<ApplianceUsage> Usages { get; set; } = new List<ApplianceUsage>();
}

/// <summary>Parameters of the solar output curve of a scenario.</summary>
public record SolarSettings
{
    /// <summary>Sunrise as hours after midnight, e.g. 6.5 = 06:30.</summary>
    public double SunriseHour { get; set; } = 6.5;
    public double SunsetHour { get; set; } = 19.5;

    /// <summary>PV output at solar noon on this day, in kW.</summary>
    public double PeakOutputKw { get; set; } = 3.0;

    /// <summary>Random reduction for passing clouds, 0 = none, 0.3 = up to 30 % less per step.</summary>
    public double CloudVariability { get; set; } = 0.1;
}

/// <summary>One planned use of an appliance on every simulated day.</summary>
public record ApplianceUsage
{
    public string DeviceId { get; set; } = "";

    /// <summary>Planned start time of day, e.g. "18:00".</summary>
    public TimeOnly Start { get; set; }

    /// <summary>The real start is moved randomly by up to this many minutes earlier or later.</summary>
    public int JitterMinutes { get; set; }

    /// <summary>Optional run length in minutes; without it the appliance's default cycle is used.</summary>
    public int? DurationMinutes { get; set; }
}
