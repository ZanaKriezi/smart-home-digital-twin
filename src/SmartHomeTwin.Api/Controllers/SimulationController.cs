using Microsoft.AspNetCore.Mvc;
using SmartHomeTwin.Api.Contracts;
using SmartHomeTwin.Core.Simulation;

namespace SmartHomeTwin.Api.Controllers;

[ApiController]
[Route("api/simulation")]
public class SimulationController : ControllerBase
{
    private readonly TwinEngine _engine;

    public SimulationController(TwinEngine engine)
    {
        _engine = engine;
    }

    [HttpGet]
    public SimulationStatus Status()
    {
        return new SimulationStatus(
            _engine.IsRunning,
            _engine.SecondsPerTick,
            (int)_engine.Clock.StepLength.TotalMinutes,
            _engine.Clock.CurrentTime,
            _engine.Clock.TickIndex,
            _engine.CurrentScenario?.Id,
            _engine.CurrentRun?.Id,
            _engine.Commands.PendingCount);
    }

    [HttpPost("start")]
    public ActionResult<SimulationStatus> Start()
    {
        if (_engine.CurrentRun == null)
            return Conflict("Start a scenario first.");
        _engine.Start();
        return Status();
    }

    [HttpPost("pause")]
    public SimulationStatus Pause()
    {
        _engine.Pause();
        return Status();
    }

    ///Runs exactly one step. Only allowed while the simulation is paused.
    [HttpPost("step")]
    public ActionResult<HomeSnapshot> Step()
    {
        if (_engine.CurrentRun == null)
            return Conflict("Start a scenario first.");
        if (_engine.IsRunning)
            return Conflict("Pause the simulation before stepping.");
        return _engine.Tick().Snapshot;
    }

    ///Sets how many real seconds one 15-minute step takes.
    [HttpPut("speed")]
    public ActionResult<SimulationStatus> Speed(SpeedRequest request)
    {
        if (request.SecondsPerTick < 0.05 || request.SecondsPerTick > 3600)
            return BadRequest("secondsPerTick must be between 0.05 and 3600.");
        _engine.SecondsPerTick = request.SecondsPerTick;
        return Status();
    }
}
