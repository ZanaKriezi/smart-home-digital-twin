namespace SmartHomeTwin.Core.Energy;

/// <summary>Technical limits of the home battery.</summary>
/// <param name="CapacityKwh">Usable capacity, 10 kWh in the prototype.</param>
/// <param name="MaxPowerKw">Maximum charge and discharge power, 5 kW in the prototype.</param>
/// <param name="MinSocPercent">The battery is never discharged below this level, 10 % in the prototype.</param>
/// <param name="ChargeEfficiency">Share of the charging energy that ends up stored (1.0 = losses ignored).</param>
/// <param name="DischargeEfficiency">Share of the stored energy that reaches the home (1.0 = losses ignored).</param>
public record BatterySpec(
    double CapacityKwh,
    double MaxPowerKw,
    double MinSocPercent,
    double ChargeEfficiency = 1.0,
    double DischargeEfficiency = 1.0)
{
    public double MinSocKwh => CapacityKwh * MinSocPercent / 100.0;

    public void Validate()
    {
        if (CapacityKwh <= 0) throw new ArgumentOutOfRangeException(nameof(CapacityKwh));
        if (MaxPowerKw <= 0) throw new ArgumentOutOfRangeException(nameof(MaxPowerKw));
        if (MinSocPercent is < 0 or >= 100) throw new ArgumentOutOfRangeException(nameof(MinSocPercent));
        if (ChargeEfficiency is <= 0 or > 1) throw new ArgumentOutOfRangeException(nameof(ChargeEfficiency));
        if (DischargeEfficiency is <= 0 or > 1) throw new ArgumentOutOfRangeException(nameof(DischargeEfficiency));
    }
}
