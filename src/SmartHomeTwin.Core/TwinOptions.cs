using SmartHomeTwin.Core.Alerts;
using SmartHomeTwin.Core.Energy;

namespace SmartHomeTwin.Core;

/// Configuration of the twin, bound from the "Twin" section of appsettings.json.
public class TwinOptions
{
    public const string SectionName = "Twin";

    ///Simulated time per step. 15 minutes, the usual smart-meter interval.
    public int StepMinutes { get; set; } = 15;

    /// Real time between two steps while the simulation runs (2 s means one day in about 3 minutes).
    public double SecondsPerTick { get; set; } = 2.0;

    /// Scenario started automatically when the application starts.
    public string DefaultScenarioId { get; set; } = "normal";

    public List<DeviceOptions> Devices { get; set; } = new List<DeviceOptions>();
    public BatteryOptions Battery { get; set; } = new();
    public AlertSettings Alerts { get; set; } = new();

    public TimeSpan StepLength => TimeSpan.FromMinutes(StepMinutes);
}

public class DeviceOptions
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    /// Fridge, Oven, Kettle, Dishwasher, WashingMachine or SolarPv.
    public string Type { get; set; } = "";

    ///Rated power in kW (peak power in kWp for SolarPv).
    public double RatedPowerKw { get; set; }
}

public class BatteryOptions
{
    public string Id { get; set; } = "battery";
    public string Name { get; set; } = "Home battery";
    public double CapacityKwh { get; set; } = 10;
    public double MaxPowerKw { get; set; } = 5;
    public double MinSocPercent { get; set; } = 10;
    public double ChargeEfficiency { get; set; } = 1.0;
    public double DischargeEfficiency { get; set; } = 1.0;

    public BatterySpec ToSpec() => new BatterySpec(CapacityKwh, MaxPowerKw, MinSocPercent, ChargeEfficiency, DischargeEfficiency);
}
