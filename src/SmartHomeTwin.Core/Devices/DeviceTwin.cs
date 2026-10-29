using SmartHomeTwin.Core.Abstractions;

namespace SmartHomeTwin.Core.Devices;


/// Common base class of all device twins. A new device type is added by deriving a new class from it;
/// the energy balance only looks at <see cref="Category"/> and <see cref="CurrentPowerKw"/>.
public abstract class DeviceTwin
{
    protected DeviceTwin(string id, string name, double ratedPowerKw)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Device id is required.", nameof(id));
        if (ratedPowerKw < 0) throw new ArgumentOutOfRangeException(nameof(ratedPowerKw));
        Id = id;
        Name = name;
        RatedPowerKw = ratedPowerKw;
    }

    public string Id { get; }
    public string Name { get; }
    public double RatedPowerKw { get; }

    /// Short type name, for example "Oven".
    public abstract string Type { get; }
    public abstract DeviceCategory Category { get; }

    /// False after the user switched the device off.
    public bool IsEnabled { get; private set; } = true;

    /// True when the device drew or produced power during the last step.
    public bool IsOn { get; private set; }

    /// Average power during the last step, in kW (always positive).
    public double CurrentPowerKw { get; private set; }

    ///Energy of the last step, in kWh.
    public double LastStepEnergyKwh { get; private set; }

    public DateTime? LastUpdated { get; private set; }

    public virtual void ApplyReading(DeviceReading reading, TimeSpan stepLength)
    {
        if (reading.DeviceId != Id)
            throw new ArgumentException($"Reading for '{reading.DeviceId}' applied to twin '{Id}'.", nameof(reading));

        var power = Math.Max(0, reading.AveragePowerKw);
        CurrentPowerKw = power;
        LastStepEnergyKwh = power * stepLength.TotalHours;
        IsOn = reading.IsOn && power > 0;
        LastUpdated = reading.Timestamp;
    }

    public virtual void SetEnabled(bool enabled) => IsEnabled = enabled;

    public virtual void Reset()
    {
        IsEnabled = true;
        IsOn = false;
        CurrentPowerKw = 0;
        LastStepEnergyKwh = 0;
        LastUpdated = null;
    }
}
