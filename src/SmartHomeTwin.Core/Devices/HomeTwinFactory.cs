namespace SmartHomeTwin.Core.Devices;

/// Builds the home twin from configuration. A new device type needs one new line here and one new class.
public static class HomeTwinFactory
{
    public static HomeTwin Create(TwinOptions options, double initialSocPercent = 50)
    {
        var devices = options.Devices.Select(CreateDevice).ToList();
        var spec = options.Battery.ToSpec();
        spec.Validate();
        var battery = new BatteryTwin(options.Battery.Id, options.Battery.Name, spec, initialSocPercent);
        return new HomeTwin(devices, battery);
    }

    public static DeviceTwin CreateDevice(DeviceOptions o) => o.Type switch
    {
        "Fridge" => new FridgeTwin(o.Id, o.Name, o.RatedPowerKw),
        "Oven" => new OvenTwin(o.Id, o.Name, o.RatedPowerKw),
        "Kettle" => new KettleTwin(o.Id, o.Name, o.RatedPowerKw),
        "Dishwasher" => new DishwasherTwin(o.Id, o.Name, o.RatedPowerKw),
        "WashingMachine" => new WashingMachineTwin(o.Id, o.Name, o.RatedPowerKw),
        "SolarPv" => new SolarPvTwin(o.Id, o.Name, o.RatedPowerKw),
        _ => throw new NotSupportedException($"Unknown device type '{o.Type}' for device '{o.Id}'.")
    };
}
