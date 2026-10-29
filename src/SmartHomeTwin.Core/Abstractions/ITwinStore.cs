using SmartHomeTwin.Core.Simulation;

namespace SmartHomeTwin.Core.Abstractions;

/// Saves the twin's history. Implemented with EF Core and SQL Server in the API project.
public interface ITwinStore
{
    void BeginRun(SimulationRun run);

    /// Saves one complete step (readings, home snapshot and new alerts) in one transaction.
    void SaveTick(TickResult tick);
}
