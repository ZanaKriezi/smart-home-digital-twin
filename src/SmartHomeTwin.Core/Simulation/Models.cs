using SmartHomeTwin.Core.Abstractions;
using SmartHomeTwin.Core.Alerts;
using SmartHomeTwin.Core.Devices;
using SmartHomeTwin.Core.Energy;

namespace SmartHomeTwin.Core.Simulation;

///One run of a scenario, from its start until another scenario is started.
public record SimulationRun(Guid Id, string ScenarioId, int Seed, DateTime SimulationStart, DateTime StartedAtUtc);

///State of one device twin at a point in time.
public record DeviceState(
    string Id,
    string Name,
    string Type,
    DeviceCategory Category,
    double RatedPowerKw,
    bool IsEnabled,
    bool IsOn,
    double PowerKw,
    double LastStepEnergyKwh,
    bool IsShiftable,
    double? StateOfChargePercent)
{
    public static DeviceState From(DeviceTwin d)
    {
        bool isShiftable = false;
        if (d is ApplianceTwin appliance)
            isShiftable = appliance.IsShiftable;

        double? stateOfCharge = null;
        if (d is BatteryTwin battery)
            stateOfCharge = battery.StateOfChargePercent;

        return new DeviceState(d.Id, d.Name, d.Type, d.Category, d.RatedPowerKw, d.IsEnabled, d.IsOn,
            d.CurrentPowerKw, d.LastStepEnergyKwh, isShiftable, stateOfCharge);
    }
}

///State of the whole home after a step.
public record HomeSnapshot(
    Guid RunId,
    string ScenarioId,
    long TickIndex,
    DateTime Timestamp,
    double LoadKw,
    double PvKw,
    double BatteryChargeKw,
    double BatteryDischargeKw,
    double BatterySocKwh,
    double BatterySocPercent,
    double GridImportKw,
    double GridExportKw,
    List<DeviceState> Devices,
    List<Alert> ActiveAlerts);

/// Everything produced by one tick of the twin loop.
public record TickResult(
    SimulationRun Run,
    long TickIndex,
    DateTime Timestamp,
    TimeSpan StepLength,
    List<DeviceReading> Readings,
    EnergyBalanceResult Balance,
    HomeSnapshot Snapshot,
    List<Alert> NewAlerts);
