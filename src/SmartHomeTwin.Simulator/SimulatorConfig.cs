using System.Text.Json;
using System.Text.Json.Serialization;
using SmartHomeTwin.Core.Scenarios;
using SmartHomeTwin.Simulator.Profiles;

namespace SmartHomeTwin.Simulator;

/// <summary>Contents of scenarios.json: the appliance load profiles and the predefined scenarios.</summary>
public class SimulatorConfig
{
    public List<ApplianceProfile> ApplianceProfiles { get; set; } = new List<ApplianceProfile>();
    public List<ScenarioDefinition> Scenarios { get; set; } = new List<ScenarioDefinition>();

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static SimulatorConfig Parse(string json) =>
        JsonSerializer.Deserialize<SimulatorConfig>(json, JsonOptions)
        ?? throw new InvalidDataException("Empty simulator configuration.");

    public static SimulatorConfig Load(string path) => Parse(File.ReadAllText(path));
}
