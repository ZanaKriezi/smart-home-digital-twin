using Microsoft.Extensions.Configuration;
using SmartHomeTwin.Core;
using SmartHomeTwin.Core.Abstractions;
using SmartHomeTwin.Core.Devices;
using SmartHomeTwin.Core.Scenarios;
using SmartHomeTwin.Core.Simulation;
using SmartHomeTwin.Simulator;

namespace SmartHomeTwin.Tests;

/// <summary>Builds the twin from the same appsettings.json and scenarios.json the API uses.</summary>
public static class TestSetup
{
    public static TwinOptions LoadOptions()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
        return config.GetSection(TwinOptions.SectionName).Get<TwinOptions>()!;
    }

    public static SimulatorConfig LoadSimulatorConfig() =>
        SimulatorConfig.Load(Path.Combine(AppContext.BaseDirectory, "scenarios.json"));

    public static ScenarioDefinition Scenario(string id) =>
        LoadSimulatorConfig().Scenarios.Single(s => s.Id == id);

    public static (TwinEngine Engine, SimulatedHome Home, RecordingStore Store, ScenarioManager Scenarios) CreateEngine()
    {
        var options = LoadOptions();
        var simConfig = LoadSimulatorConfig();
        var simulated = new SimulatedHome(options, simConfig.ApplianceProfiles);
        var store = new RecordingStore();
        var engine = new TwinEngine(options, HomeTwinFactory.Create(options), simulated, store, new CommandHandler());
        return (engine, simulated, store, new ScenarioManager(simConfig.Scenarios, engine));
    }
}

/// <summary>Keeps saved ticks in memory instead of a database.</summary>
public sealed class RecordingStore : ITwinStore
{
    public List<SimulationRun> Runs { get; } = [];
    public List<TickResult> Ticks { get; } = [];

    public void BeginRun(SimulationRun run)
    {
        Runs.Add(run);
    }

    public void SaveTick(TickResult tick)
    {
        Ticks.Add(tick);
    }
}
