using Microsoft.AspNetCore.Mvc;
using SmartHomeTwin.Api.Contracts;
using SmartHomeTwin.Core.Abstractions;
using SmartHomeTwin.Core.Devices;
using SmartHomeTwin.Core.Simulation;

namespace SmartHomeTwin.Api.Controllers;

[ApiController]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly TwinEngine _engine;

    public DevicesController(TwinEngine engine)
    {
        _engine = engine;
    }

    /// All device twins with their current state.
    [HttpGet]
    public ActionResult<List<DeviceState>> GetAll()
    {
        if (_engine.LatestSnapshot == null)
            return NotFound("No scenario has been started yet.");
        return _engine.LatestSnapshot.Devices;
    }

    [HttpGet("{id}")]
    public ActionResult<DeviceState> Get(string id)
    {
        DeviceState? device = _engine.LatestSnapshot?.Devices.FirstOrDefault(d => d.Id == id);
        if (device == null)
            return NotFound();
        return device;
    }

    ///Switches a device on or off. The command takes effect in the next simulation step.
    [HttpPost("{id}/commands")]
    public IActionResult SendCommand(string id, DeviceCommandRequest request)
    {
        DeviceTwin? device = _engine.Home.Find(id);
        if (device == null)
            return NotFound();

        _engine.Commands.Submit(new DeviceCommand(device.Id, request.Action));
        return Accepted(new { deviceId = device.Id, action = request.Action, effectiveFrom = _engine.Clock.CurrentTime });
    }
}
