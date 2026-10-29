namespace SmartHomeTwin.Simulator.Profiles;

/// <summary>One run of an appliance program, either planned for the day or started by a user command.</summary>
public class ApplianceRun
{
    public ApplianceRun(string deviceId, DateTime start, List<LoadPhase> phases)
    {
        DeviceId = deviceId;
        Start = start;
        Phases = phases;
        End = start.AddMinutes(phases.Sum(p => p.Minutes));
    }

    public string DeviceId { get; }
    public DateTime Start { get; }
    public DateTime End { get; }
    public List<LoadPhase> Phases { get; }

    /// <summary>Power at the given minute, or 0 when the run is not active at that minute.</summary>
    public double PowerAt(DateTime minute)
    {
        if (minute < Start || minute >= End)
            return 0;

        double minutesSinceStart = (minute - Start).TotalMinutes;
        foreach (LoadPhase phase in Phases)
        {
            if (minutesSinceStart < phase.Minutes)
                return phase.PowerKw;
            minutesSinceStart -= phase.Minutes;
        }
        return 0;
    }
}
