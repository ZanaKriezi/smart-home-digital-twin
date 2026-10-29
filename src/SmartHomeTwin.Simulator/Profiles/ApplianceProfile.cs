namespace SmartHomeTwin.Simulator.Profiles;

public enum ProfileMode
{
    /// <summary>The appliance runs one program (a list of phases) each time it is used, e.g. oven or dishwasher.</summary>
    Program,

    /// <summary>The appliance is always on and its compressor switches on and off in short cycles, e.g. the fridge.</summary>
    Cooling
}

/// <summary>Constant power for a number of minutes, one part of an appliance program.</summary>
public record LoadPhase(int Minutes, double PowerKw);

/// <summary>Synthetic load profile of one appliance.</summary>
public record ApplianceProfile
{
    public string DeviceId { get; set; } = "";
    public ProfileMode Mode { get; set; } = ProfileMode.Program;

    /// <summary>Program phases in order (Program mode). Their sum is the default cycle length.</summary>
    public List<LoadPhase> Phases { get; set; } = new List<LoadPhase>();

    /// <summary>Compressor power (Cooling mode).</summary>
    public double CoolingPowerKw { get; set; }

    /// <summary>Length of one on/off compressor cycle in minutes (Cooling mode).</summary>
    public int CoolingCycleMinutes { get; set; } = 40;

    /// <summary>Share of each cycle in which the compressor runs (Cooling mode).</summary>
    public double CoolingDutyCycle { get; set; } = 0.35;

    public int DefaultDurationMinutes => Phases.Sum(p => p.Minutes);

    /// <summary>
    /// Returns the program phases for a run of the given length: phases are cut when the run is shorter,
    /// and the last phase is extended when it is longer.
    /// </summary>
    public List<LoadPhase> PhasesFor(int? durationMinutes)
    {
        if (durationMinutes == null || Phases.Count == 0)
            return Phases;
        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));

        var result = new List<LoadPhase>();
        int remaining = durationMinutes.Value;
        foreach (LoadPhase phase in Phases)
        {
            if (remaining <= 0)
                break;
            int minutes = Math.Min(phase.Minutes, remaining);
            result.Add(new LoadPhase(minutes, phase.PowerKw));
            remaining -= minutes;
        }

        // The run is longer than the program: the last phase lasts longer.
        if (remaining > 0)
        {
            LoadPhase last = result[result.Count - 1];
            result[result.Count - 1] = new LoadPhase(last.Minutes + remaining, last.PowerKw);
        }
        return result;
    }
}
