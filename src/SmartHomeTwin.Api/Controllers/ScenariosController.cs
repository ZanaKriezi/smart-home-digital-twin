using Microsoft.AspNetCore.Mvc;
using SmartHomeTwin.Api.Contracts;
using SmartHomeTwin.Core.Scenarios;
using SmartHomeTwin.Core.Simulation;

namespace SmartHomeTwin.Api.Controllers;

[ApiController]
[Route("api/scenarios")]
public class ScenariosController : ControllerBase
{
    private readonly ScenarioManager _scenarios;
    private readonly TwinEngine _engine;

    public ScenariosController(ScenarioManager scenarios, TwinEngine engine)
    {
        _scenarios = scenarios;
        _engine = engine;
    }

    [HttpGet]
    public List<ScenarioDto> GetAll()
    {
        var result = new List<ScenarioDto>();
        foreach (ScenarioDefinition s in _scenarios.All)
        {
            bool isActive = _engine.CurrentScenario?.Id == s.Id;
            result.Add(new ScenarioDto(s.Id, s.Name, s.Description, s.Seed, s.StartDate, s.InitialBatterySocPercent, isActive));
        }
        return result;
    }

    ///Resets the twin and starts a new run of the scenario.
    [HttpPost("{id}/start")]
    public ActionResult<SimulationRun> Start(string id)
    {
        if (_scenarios.Find(id) == null)
            return NotFound();
        return _scenarios.Start(id);
    }
}
