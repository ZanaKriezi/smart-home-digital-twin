namespace SmartHomeTwin.Core.Devices;

/// Rooftop PV system. Its rated power is the peak power in kWp.
public class SolarPvTwin : DeviceTwin
{
    public SolarPvTwin(string id, string name, double peakPowerKwp) : base(id, name, peakPowerKwp)
    {
    }

    public override string Type => "SolarPv";
    public override DeviceCategory Category => DeviceCategory.Producer;
}
