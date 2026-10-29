using SmartHomeTwin.Core.Abstractions;
using SmartHomeTwin.Core.Energy;

namespace SmartHomeTwin.Core.Devices;

/// Home battery. Unlike the other devices it gets no reading from the data source:
/// its charge and discharge follow from the energy balance of the home.

public class BatteryTwin : DeviceTwin
{
    public BatteryTwin(string id, string name, BatterySpec spec, double initialSocPercent)
        : base(id, name, spec.MaxPowerKw)
    {
        Spec = spec;
        SetStateOfChargePercent(initialSocPercent);
    }

    public BatterySpec Spec { get; }
    public override string Type => "Battery";
    public override DeviceCategory Category => DeviceCategory.Storage;

    public double StateOfChargeKwh { get; private set; }
    public double StateOfChargePercent => Spec.CapacityKwh == 0 ? 0 : 100.0 * StateOfChargeKwh / Spec.CapacityKwh;

    ///Positive while charging, in kW.
    public double ChargePowerKw { get; private set; }

    ///Positive while discharging, in kW.
    public double DischargePowerKw { get; private set; }

    public void SetStateOfChargePercent(double percent)
    {
        if (percent is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(percent));
        StateOfChargeKwh = Spec.CapacityKwh * percent / 100.0;
    }

    ///Takes over the battery part of an energy balance result.
    public void ApplyBalance(EnergyBalanceResult balance, DateTime timestamp, TimeSpan stepLength)
    {
        ChargePowerKw = balance.BatteryChargeKw;
        DischargePowerKw = balance.BatteryDischargeKw;
        StateOfChargeKwh = balance.BatterySocAfterKwh;
        var power = ChargePowerKw + DischargePowerKw;
        base.ApplyReading(new DeviceReading(Id, timestamp, power, power > 0), stepLength);
    }

    public override void ApplyReading(DeviceReading reading, TimeSpan stepLength) =>
        throw new InvalidOperationException("The battery state is calculated by the energy balance, not read from the data source.");

    public void Reset(double initialSocPercent)
    {
        base.Reset();
        ChargePowerKw = 0;
        DischargePowerKw = 0;
        SetStateOfChargePercent(initialSocPercent);
    }
}
