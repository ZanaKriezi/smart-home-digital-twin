namespace SmartHomeTwin.Core.Simulation;

/// Simulated time. Each tick covers one step of <see cref="StepLength"/> (15 minutes),
/// independent of how fast the simulation runs in real time.

public class SimulationClock
{
    public SimulationClock(TimeSpan stepLength)
    {
        if (stepLength <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(stepLength));
        StepLength = stepLength;
    }

    public TimeSpan StepLength { get; }

    ///Simulated time at which the run started.
    public DateTime StartTime { get; private set; }

    ///Start of the next step to simulate.
    public DateTime CurrentTime { get; private set; }

    ///Number of steps simulated since the start of the run.
    public long TickIndex { get; private set; }

    public void Reset(DateTime startTime)
    {
        StartTime = startTime;
        CurrentTime = startTime;
        TickIndex = 0;
    }

    public void Advance()
    {
        CurrentTime += StepLength;
        TickIndex++;
    }
}
