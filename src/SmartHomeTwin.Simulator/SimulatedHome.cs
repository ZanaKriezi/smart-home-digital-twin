using SmartHomeTwin.Core;
using SmartHomeTwin.Core.Abstractions;
using SmartHomeTwin.Core.Scenarios;
using SmartHomeTwin.Simulator.Profiles;
using SmartHomeTwin.Simulator.Solar;

namespace SmartHomeTwin.Simulator;

/// <summary>
/// The simulated home behind the twin. For every step it produces one reading per device from the
/// appliance load profiles, the solar curve of the active scenario and the user's commands.
///
/// Appliances are simulated minute by minute and the result is averaged over the step.
/// Example: a kettle (2 kW) that runs 3 minutes of a 15-minute step gives 2 × 3 / 15 = 0.4 kW on average,
/// which is 0.4 kW × 0.25 h = 0.1 kWh.
///
/// All random numbers come from the scenario's seed, so the same scenario always gives the same result.
/// </summary>
public class SimulatedHome : IDeviceDataSource
{
    // The API may send commands while the background loop reads data, so both lock this object.
    private readonly object _sync = new object();

    private readonly Dictionary<string, ApplianceProfile> _profiles = new Dictionary<string, ApplianceProfile>();
    private readonly string? _solarDeviceId;

    // Program runs that are planned or in progress (oven, kettle, dishwasher, washing machine).
    private readonly List<ApplianceRun> _runs = new List<ApplianceRun>();

    // Devices the user switched off, and program appliances the user asked to start.
    private readonly HashSet<string> _switchedOff = new HashSet<string>();
    private readonly HashSet<string> _startRequests = new HashSet<string>();

    private ScenarioDefinition? _scenario;
    private SolarCurve? _solar;
    private DateOnly? _lastPlannedDay;
    private int _fridgeOffsetMinutes;

    public SimulatedHome(TwinOptions options, List<ApplianceProfile> profiles)
    {
        foreach (DeviceOptions device in options.Devices)
        {
            if (device.Type == "SolarPv")
            {
                _solarDeviceId = device.Id;
                continue;
            }

            ApplianceProfile? profile = profiles.FirstOrDefault(p => p.DeviceId == device.Id);
            if (profile == null)
                throw new InvalidOperationException($"No load profile configured for: {device.Id}.");
            _profiles[device.Id] = profile;
        }
    }

    public void Reset(ScenarioDefinition scenario)
    {
        lock (_sync)
        {
            _scenario = scenario;
            _solar = new SolarCurve(scenario.Solar);
            _runs.Clear();
            _switchedOff.Clear();
            _startRequests.Clear();
            _lastPlannedDay = null;
            _fridgeOffsetMinutes = new Random(scenario.Seed).Next(0, 60);
        }
    }

    public void SendCommand(DeviceCommand command)
    {
        lock (_sync)
        {
            string id = command.DeviceId;
            if (command.Action == CommandAction.Off)
            {
                // Switching off stops the device at once and cancels its current run.
                _switchedOff.Add(id);
                _startRequests.Remove(id);
                _runs.RemoveAll(r => r.DeviceId == id);
            }
            else
            {
                // Switching on lets the device run again; a program appliance starts one program.
                _switchedOff.Remove(id);
                if (_profiles.ContainsKey(id) && _profiles[id].Mode == ProfileMode.Program)
                    _startRequests.Add(id);
            }
        }
    }

    public List<DeviceReading> ReadAll(DateTime stepStart, TimeSpan stepLength)
    {
        lock (_sync)
        {
            if (_scenario == null || _solar == null)
                throw new InvalidOperationException("Call Reset with a scenario first.");

            DateTime stepEnd = stepStart + stepLength;
            int minutes = (int)stepLength.TotalMinutes;

            PlanDaysUntil(DateOnly.FromDateTime(stepEnd.AddMinutes(-1)));
            StartRequestedPrograms(stepStart);

            // Runs planned while a device is switched off are skipped.
            _runs.RemoveAll(r => _switchedOff.Contains(r.DeviceId) && r.Start < stepEnd);

            var readings = new List<DeviceReading>();

            // Appliances: add up the power of every minute, then take the average.
            foreach (ApplianceProfile profile in _profiles.Values)
            {
                double total = 0;
                if (!_switchedOff.Contains(profile.DeviceId))
                {
                    for (int m = 0; m < minutes; m++)
                        total += PowerAt(profile, stepStart.AddMinutes(m));
                }
                double average = total / minutes;
                readings.Add(new DeviceReading(profile.DeviceId, stepStart, average, average > 0));
            }

            // Solar: average of the curve over the step, reduced a little by random clouds.
            if (_solarDeviceId != null)
            {
                double average = 0;
                if (!_switchedOff.Contains(_solarDeviceId))
                {
                    for (int m = 0; m < minutes; m++)
                        average += _solar.PowerKwAt(stepStart.AddMinutes(m + 0.5));
                    average = average / minutes * CloudFactor(stepStart);
                }
                readings.Add(new DeviceReading(_solarDeviceId, stepStart, average, average > 0));
            }

            // Forget runs that have finished.
            _runs.RemoveAll(r => r.End <= stepEnd);
            return readings;
        }
    }

    /// <summary>Runs that are planned or in progress (used by the tests).</summary>
    public List<ApplianceRun> PlannedRuns
    {
        get
        {
            lock (_sync)
            {
                return _runs.ToList();
            }
        }
    }

    private double PowerAt(ApplianceProfile profile, DateTime minute)
    {
        if (profile.Mode == ProfileMode.Cooling)
        {
            // The fridge compressor repeats the same on/off cycle all day.
            long minuteNumber = (long)(minute - DateTime.UnixEpoch).TotalMinutes + _fridgeOffsetMinutes;
            double onMinutes = profile.CoolingCycleMinutes * profile.CoolingDutyCycle;
            bool compressorOn = minuteNumber % profile.CoolingCycleMinutes < onMinutes;
            return compressorOn ? profile.CoolingPowerKw : 0;
        }

        double power = 0;
        foreach (ApplianceRun run in _runs)
        {
            if (run.DeviceId == profile.DeviceId)
                power += run.PowerAt(minute);
        }
        return power;
    }

    /// <summary>Creates the planned appliance runs for every day up to and including the given day.</summary>
    private void PlanDaysUntil(DateOnly day)
    {
        ScenarioDefinition scenario = _scenario!;
        DateOnly next = _lastPlannedDay == null ? scenario.StartDate : _lastPlannedDay.Value.AddDays(1);

        for (DateOnly d = next; d <= day; d = d.AddDays(1))
        {
            // A new random generator per day, so each day is the same whatever the user did before.
            int dayNumber = d.DayNumber - scenario.StartDate.DayNumber;
            var random = new Random(scenario.Seed * 7919 + dayNumber);

            foreach (ApplianceUsage usage in scenario.Usages)
            {
                int shift = random.Next(-usage.JitterMinutes, usage.JitterMinutes + 1);
                if (!_profiles.TryGetValue(usage.DeviceId, out ApplianceProfile? profile) || profile.Mode != ProfileMode.Program)
                    continue;

                DateTime start = d.ToDateTime(usage.Start).AddMinutes(shift);
                _runs.Add(new ApplianceRun(profile.DeviceId, start, profile.PhasesFor(usage.DurationMinutes)));
            }
            _lastPlannedDay = d;
        }
    }

    private void StartRequestedPrograms(DateTime stepStart)
    {
        foreach (string id in _startRequests)
        {
            bool alreadyRunning = _runs.Any(r => r.DeviceId == id && r.Start <= stepStart && r.End > stepStart);
            if (!alreadyRunning)
                _runs.Add(new ApplianceRun(id, stepStart, _profiles[id].Phases));
        }
        _startRequests.Clear();
    }

    /// <summary>Random reduction of solar output for passing clouds. The same step always gets the same value.</summary>
    private double CloudFactor(DateTime stepStart)
    {
        ScenarioDefinition scenario = _scenario!;
        if (scenario.Solar.CloudVariability <= 0)
            return 1.0;

        int minutesSinceStart = (int)(stepStart - scenario.StartDate.ToDateTime(TimeOnly.MinValue)).TotalMinutes;
        var random = new Random(scenario.Seed * 31 + minutesSinceStart);
        return 1.0 - scenario.Solar.CloudVariability * random.NextDouble();
    }
}
