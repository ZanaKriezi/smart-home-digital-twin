using SmartHomeTwin.Core.Energy;

namespace SmartHomeTwin.Core.Alerts;


/// Checks the alert thresholds at every step. An alert is reported once, in the step where its condition starts,
/// and stays in ActiveAlerts for as long as the condition lasts.

public class AlertService
{
    private readonly Dictionary<AlertType, Alert> _active = new Dictionary<AlertType, Alert>();

    public AlertService(AlertSettings settings)
    {
        Settings = settings;
    }

    public AlertSettings Settings { get; }

    public List<Alert> ActiveAlerts => _active.Values.ToList();

    /// Checks one step and returns the alerts that started in it.
    public List<Alert> Evaluate(EnergyBalanceResult balance, BatterySpec battery, DateTime timestamp)
    {
        var started = new List<Alert>();

        bool highImport = balance.GridImportKw > Settings.GridImportLimitKw;
        Update(AlertType.HighGridImport, highImport, started, new Alert(AlertType.HighGridImport, timestamp,
            $"Grid import of {balance.GridImportKw:0.00} kW is above the limit of {Settings.GridImportLimitKw:0.00} kW.",
            balance.GridImportKw, Settings.GridImportLimitKw));

        bool highLoad = balance.LoadKw > Settings.HighLoadKw;
        Update(AlertType.HighLoad, highLoad, started, new Alert(AlertType.HighLoad, timestamp,
            $"Home load of {balance.LoadKw:0.00} kW is above {Settings.HighLoadKw:0.00} kW.",
            balance.LoadKw, Settings.HighLoadKw));

        // The battery is "at minimum" from the step it reaches its reserve until it is charged again.
        bool atMinimum = balance.BatterySocAfterKwh <= battery.MinSocKwh + 0.000001;
        Update(AlertType.BatteryAtMinimum, atMinimum, started, new Alert(AlertType.BatteryAtMinimum, timestamp,
            $"Battery reached its minimum state of charge of {battery.MinSocPercent:0} %; further deficits are imported from the grid.",
            100.0 * balance.BatterySocAfterKwh / battery.CapacityKwh, battery.MinSocPercent));

        return started;
    }

    public void Reset()
    {
        _active.Clear();
    }

    private void Update(AlertType type, bool conditionIsTrue, List<Alert> started, Alert alert)
    {
        if (conditionIsTrue && !_active.ContainsKey(type))
        {
            _active[type] = alert;   // the condition has just started
            started.Add(alert);
        }
        else if (!conditionIsTrue)
        {
            _active.Remove(type);    // the condition is over
        }
    }
}
