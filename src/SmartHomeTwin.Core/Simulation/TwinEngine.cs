using SmartHomeTwin.Core.Abstractions;
using SmartHomeTwin.Core.Alerts;
using SmartHomeTwin.Core.Devices;
using SmartHomeTwin.Core.Energy;
using SmartHomeTwin.Core.Scenarios;

namespace SmartHomeTwin.Core.Simulation;

/// The twin loop. Every tick: apply pending commands, read new device data, update the device twins,
/// calculate the energy balance, indicators and alerts, and save the step.
/// The background service in the API calls <see cref="Tick"/> again and again while the simulation runs.

public class TwinEngine
{
    // The background loop and the API requests run on different threads.
    // Every method that reads or changes the twin locks this object, so only one of them runs at a time.
    private readonly object _sync = new object();

    private readonly IDeviceDataSource _source;
    private readonly ITwinStore _store;
    private readonly AlertService _alerts;
    private readonly KpiAccumulator _runKpis = new KpiAccumulator();
    private readonly List<Alert> _recentAlerts = new List<Alert>();
    private double _secondsPerTick;

    public TwinEngine(TwinOptions options, HomeTwin home, IDeviceDataSource source, ITwinStore store, CommandHandler commands)
    {
        Home = home;
        _source = source;
        _store = store;
        Commands = commands;
        Clock = new SimulationClock(options.StepLength);
        _alerts = new AlertService(options.Alerts);
        _secondsPerTick = options.SecondsPerTick;
    }

    public HomeTwin Home { get; }
    public SimulationClock Clock { get; }
    public CommandHandler Commands { get; }

    public SimulationRun? CurrentRun { get; private set; }
    public ScenarioDefinition? CurrentScenario { get; private set; }

    ///The home state after the last tick. It is replaced as a whole, so readers never see a half-updated state.
    public HomeSnapshot? LatestSnapshot { get; private set; }

    public bool IsRunning { get; private set; }

    ///Real seconds between two ticks while the simulation runs.
    public double SecondsPerTick
    {
        get { return _secondsPerTick; }
        set
        {
            if (value < 0.05 || value > 3600)
                throw new ArgumentOutOfRangeException(nameof(value), "Seconds per tick must be between 0.05 and 3600.");
            _secondsPerTick = value;
        }
    }

    public KpiSummary GetRunKpis()
    {
        lock (_sync)
        {
            return _runKpis.GetSummary();
        }
    }

    public List<Alert> GetRecentAlerts()
    {
        lock (_sync)
        {
            return _recentAlerts.ToList();
        }
    }

    public void Start()
    {
        if (CurrentRun == null)
            throw new InvalidOperationException("Start a scenario first.");
        IsRunning = true;
    }

    public void Pause()
    {
        IsRunning = false;
    }

    ///Resets the twin and the data source and begins a new run of the scenario. The simulation is left paused.
    public SimulationRun StartScenario(ScenarioDefinition scenario)
    {
        lock (_sync)
        {
            IsRunning = false;
            DateTime start = scenario.StartDate.ToDateTime(TimeOnly.MinValue);
            var run = new SimulationRun(Guid.NewGuid(), scenario.Id, scenario.Seed, start, DateTime.UtcNow);

            Commands.Clear();
            Clock.Reset(start);
            Home.Reset(scenario.InitialBatterySocPercent);
            _source.Reset(scenario);
            _alerts.Reset();
            _runKpis.Reset();
            _recentAlerts.Clear();

            _store.BeginRun(run);

            CurrentRun = run;
            CurrentScenario = scenario;
            LatestSnapshot = BuildSnapshot(run, -1, start, null);
            return run;
        }
    }

    /// Runs one step (15 simulated minutes) of the twin loop.
    public TickResult Tick()
    {
        lock (_sync)
        {
            if (CurrentRun == null)
                throw new InvalidOperationException("Start a scenario first.");

            DateTime timestamp = Clock.CurrentTime;
            TimeSpan step = Clock.StepLength;

            // 1. Commands sent since the last tick take effect now.
            ApplyCommands();

            // 2. Get one reading per device from the (simulated) home.
            List<DeviceReading> readings = _source.ReadAll(timestamp, step);

            // 3. Update the device twins.
            Home.ApplyReadings(readings, step);

            // 4. Energy balance of the whole home, then indicators and alerts.
            EnergyBalanceResult balance = EnergyBalanceCalculator.Calculate(
                Home.LoadKw, Home.ProductionKw, Home.Battery.Spec, Home.Battery.StateOfChargeKwh, step, Home.Battery.IsEnabled);
            Home.Battery.ApplyBalance(balance, timestamp, step);

            var applianceEnergy = new Dictionary<string, double>();
            foreach (DeviceTwin device in Home.Consumers)
                applianceEnergy[device.Id] = device.LastStepEnergyKwh;
            _runKpis.Add(balance, step, applianceEnergy);

            List<Alert> newAlerts = _alerts.Evaluate(balance, Home.Battery.Spec, timestamp);
            _recentAlerts.AddRange(newAlerts);
            if (_recentAlerts.Count > 100)
                _recentAlerts.RemoveRange(0, _recentAlerts.Count - 100);

            HomeSnapshot snapshot = BuildSnapshot(CurrentRun, Clock.TickIndex, timestamp, balance);
            var result = new TickResult(CurrentRun, Clock.TickIndex, timestamp, step, readings, balance, snapshot, newAlerts);

            // 5. Show the new state and move the clock on, then save the step.
            //    If saving fails, the step is missing in the database but the twin carries on with the next step.
            LatestSnapshot = snapshot;
            Clock.Advance();
            _store.SaveTick(result);
            return result;
        }
    }

    private void ApplyCommands()
    {
        foreach (DeviceCommand command in Commands.TakeAll())
        {
            DeviceTwin? device = Home.Find(command.DeviceId);
            if (device == null)
                continue;

            device.SetEnabled(command.Action == CommandAction.On);

            // The battery is controlled by the twin itself; all other commands go to the home.
            if (device != Home.Battery)
                _source.SendCommand(command);
        }
    }

    private HomeSnapshot BuildSnapshot(SimulationRun run, long tickIndex, DateTime timestamp, EnergyBalanceResult? balance)
    {
        var devices = new List<DeviceState>();
        foreach (DeviceTwin device in Home.Devices)
            devices.Add(DeviceState.From(device));

        return new HomeSnapshot(
            RunId: run.Id,
            ScenarioId: run.ScenarioId,
            TickIndex: tickIndex,
            Timestamp: timestamp,
            LoadKw: balance?.LoadKw ?? 0,
            PvKw: balance?.PvKw ?? 0,
            BatteryChargeKw: balance?.BatteryChargeKw ?? 0,
            BatteryDischargeKw: balance?.BatteryDischargeKw ?? 0,
            BatterySocKwh: Home.Battery.StateOfChargeKwh,
            BatterySocPercent: Home.Battery.StateOfChargePercent,
            GridImportKw: balance?.GridImportKw ?? 0,
            GridExportKw: balance?.GridExportKw ?? 0,
            Devices: devices,
            ActiveAlerts: _alerts.ActiveAlerts.ToList());
    }
}
