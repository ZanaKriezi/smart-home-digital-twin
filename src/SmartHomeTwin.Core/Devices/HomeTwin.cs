using SmartHomeTwin.Core.Abstractions;

namespace SmartHomeTwin.Core.Devices;

/// The twin of the whole home: all device twins plus the battery.
public class HomeTwin
{
    private readonly Dictionary<string, DeviceTwin> _devices;

    public HomeTwin(IEnumerable<DeviceTwin> devices, BatteryTwin battery)
    {
        Battery = battery;
        _devices = new Dictionary<string, DeviceTwin>(StringComparer.OrdinalIgnoreCase);
        foreach (DeviceTwin device in devices.Append(battery))
        {
            if (_devices.ContainsKey(device.Id))
                throw new ArgumentException($"Duplicate device id '{device.Id}'.");
            _devices.Add(device.Id, device);
        }
    }

    public BatteryTwin Battery { get; }

    public List<DeviceTwin> Devices => _devices.Values.ToList();

    public IEnumerable<DeviceTwin> Consumers => _devices.Values.Where(d => d.Category == DeviceCategory.Consumer);
    public IEnumerable<DeviceTwin> Producers => _devices.Values.Where(d => d.Category == DeviceCategory.Producer);

    public DeviceTwin? Find(string id)
    {
        _devices.TryGetValue(id, out DeviceTwin? device);
        return device;
    }

    /// Total consumption of all appliances in the last step, in kW.
    public double LoadKw => Consumers.Sum(d => d.CurrentPowerKw);

    /// Total production in the last step, in kW.
    public double ProductionKw => Producers.Sum(d => d.CurrentPowerKw);

    ///Updates the device twins with new readings. Readings for unknown devices are ignored.
    public void ApplyReadings(List<DeviceReading> readings, TimeSpan stepLength)
    {
        foreach (var reading in readings)
        {
            DeviceTwin? device = Find(reading.DeviceId);
            if (device != null && device != Battery)
                device.ApplyReading(reading, stepLength);
        }
    }

    public void Reset(double initialSocPercent)
    {
        foreach (DeviceTwin device in _devices.Values)
        {
            if (device != Battery)
                device.Reset();
        }
        Battery.Reset(initialSocPercent);
    }
}
