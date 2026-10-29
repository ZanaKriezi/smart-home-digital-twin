using SmartHomeTwin.Core.Simulation;

namespace SmartHomeTwin.Core.Scenarios;

/// <summary>Knows the predefined scenarios and starts them on the twin engine.</summary>
public class ScenarioManager
{
    private readonly List<ScenarioDefinition> _scenarios;
    private readonly TwinEngine _engine;

    public ScenarioManager(List<ScenarioDefinition> scenarios, TwinEngine engine)
    {
        _scenarios = scenarios;
        _engine = engine;
    }

    public List<ScenarioDefinition> All => _scenarios;

    public ScenarioDefinition? Find(string id)
    {
        return _scenarios.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Starts a new run of the scenario. When autoRun is true the simulation starts running at once.</summary>
    public SimulationRun Start(string id, bool autoRun = true)
    {
        ScenarioDefinition scenario = Find(id) ?? throw new KeyNotFoundException($"Unknown scenario '{id}'.");
        SimulationRun run = _engine.StartScenario(scenario);
        if (autoRun)
            _engine.Start();
        return run;
    }
}
