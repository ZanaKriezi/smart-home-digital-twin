using SmartHomeTwin.Core.Energy;

namespace SmartHomeTwin.Tests;

public class EnergyBalanceTests
{
    private static readonly BatterySpec Battery = new(CapacityKwh: 10, MaxPowerKw: 5, MinSocPercent: 10);
    private static readonly TimeSpan Step = TimeSpan.FromMinutes(15);

    [Fact]
    public void Solar_covers_load_first_and_surplus_charges_battery()
    {
        var r = EnergyBalanceCalculator.Calculate(loadKw: 1.0, pvKw: 4.0, Battery, socKwh: 5, Step);

        Assert.Equal(1.0, r.PvToLoadKw, 6);
        Assert.Equal(3.0, r.BatteryChargeKw, 6);
        Assert.Equal(0, r.GridExportKw, 6);
        Assert.Equal(0, r.GridImportKw, 6);
        Assert.Equal(5 + 3.0 * 0.25, r.BatterySocAfterKwh, 6);
    }

    [Fact]
    public void Surplus_above_battery_max_power_is_exported()
    {
        var r = EnergyBalanceCalculator.Calculate(loadKw: 0.5, pvKw: 7.0, Battery, socKwh: 2, Step);

        Assert.Equal(5.0, r.BatteryChargeKw, 6);
        Assert.Equal(1.5, r.GridExportKw, 6);
    }

    [Fact]
    public void Full_battery_exports_whole_surplus()
    {
        var r = EnergyBalanceCalculator.Calculate(loadKw: 1.0, pvKw: 4.0, Battery, socKwh: 10, Step);

        Assert.Equal(0, r.BatteryChargeKw, 6);
        Assert.Equal(3.0, r.GridExportKw, 6);
        Assert.Equal(10, r.BatterySocAfterKwh, 6);
    }

    [Fact]
    public void Battery_is_charged_only_up_to_capacity()
    {
        // 0.5 kWh room left: at most 2 kW over 15 minutes.
        var r = EnergyBalanceCalculator.Calculate(loadKw: 0, pvKw: 4.0, Battery, socKwh: 9.5, Step);

        Assert.Equal(2.0, r.BatteryChargeKw, 6);
        Assert.Equal(2.0, r.GridExportKw, 6);
        Assert.Equal(10, r.BatterySocAfterKwh, 6);
    }

    [Fact]
    public void Deficit_is_covered_by_battery_then_grid()
    {
        var r = EnergyBalanceCalculator.Calculate(loadKw: 6.0, pvKw: 0.5, Battery, socKwh: 8, Step);

        Assert.Equal(5.0, r.BatteryDischargeKw, 6); // limited by max power
        Assert.Equal(0.5, r.GridImportKw, 6);
        Assert.Equal(8 - 5.0 * 0.25, r.BatterySocAfterKwh, 6);
    }

    [Fact]
    public void Battery_never_goes_below_minimum_state_of_charge()
    {
        // 1.25 kWh in the battery, 1.0 kWh reserve: only 0.25 kWh = 1 kW over 15 minutes is available.
        var r = EnergyBalanceCalculator.Calculate(loadKw: 3.0, pvKw: 0, Battery, socKwh: 1.25, Step);

        Assert.Equal(1.0, r.BatteryDischargeKw, 6);
        Assert.Equal(2.0, r.GridImportKw, 6);
        Assert.Equal(Battery.MinSocKwh, r.BatterySocAfterKwh, 6);
    }

    [Fact]
    public void Battery_at_minimum_gives_nothing()
    {
        var r = EnergyBalanceCalculator.Calculate(loadKw: 2.0, pvKw: 0, Battery, socKwh: 1.0, Step);

        Assert.Equal(0, r.BatteryDischargeKw, 6);
        Assert.Equal(2.0, r.GridImportKw, 6);
    }

    [Fact]
    public void Switched_off_battery_neither_charges_nor_discharges()
    {
        var surplus = EnergyBalanceCalculator.Calculate(1, 4, Battery, 5, Step, batteryEnabled: false);
        var deficit = EnergyBalanceCalculator.Calculate(4, 1, Battery, 5, Step, batteryEnabled: false);

        Assert.Equal(3.0, surplus.GridExportKw, 6);
        Assert.Equal(0, surplus.BatteryChargeKw, 6);
        Assert.Equal(3.0, deficit.GridImportKw, 6);
        Assert.Equal(0, deficit.BatteryDischargeKw, 6);
        Assert.Equal(5, deficit.BatterySocAfterKwh, 6);
    }

    [Fact]
    public void Night_with_no_load_changes_nothing()
    {
        var r = EnergyBalanceCalculator.Calculate(0, 0, Battery, 5, Step);

        Assert.Equal(0, r.GridImportKw + r.GridExportKw + r.BatteryChargeKw + r.BatteryDischargeKw, 6);
        Assert.Equal(5, r.BatterySocAfterKwh, 6);
    }

    [Theory]
    [InlineData(1.0, 1.0)]
    [InlineData(0.95, 0.95)]
    [InlineData(0.9, 0.85)]
    public void Power_balance_and_battery_limits_hold_for_random_inputs(double chargeEff, double dischargeEff)
    {
        var battery = Battery with { ChargeEfficiency = chargeEff, DischargeEfficiency = dischargeEff };
        var random = new Random(1);

        for (var i = 0; i < 10_000; i++)
        {
            var load = random.NextDouble() * 12;
            var pv = random.NextDouble() * 6;
            var soc = random.NextDouble() * battery.CapacityKwh;
            var r = EnergyBalanceCalculator.Calculate(load, pv, battery, soc, Step);

            // P_PV + P_import + P_discharge = P_load + P_charge + P_export
            Assert.Equal(r.PvKw + r.GridImportKw + r.BatteryDischargeKw, r.LoadKw + r.BatteryChargeKw + r.GridExportKw, 9);

            Assert.True(r.GridImportKw >= 0 && r.GridExportKw >= 0);
            Assert.False(r.GridImportKw > 1e-9 && r.GridExportKw > 1e-9, "import and export at the same time");
            Assert.False(r.BatteryChargeKw > 1e-9 && r.BatteryDischargeKw > 1e-9, "charge and discharge at the same time");
            Assert.InRange(r.BatteryChargeKw, 0, battery.MaxPowerKw + 1e-9);
            Assert.InRange(r.BatteryDischargeKw, 0, battery.MaxPowerKw + 1e-9);
            Assert.InRange(r.BatterySocAfterKwh, 0, battery.CapacityKwh + 1e-9);
            if (soc >= battery.MinSocKwh)
                Assert.True(r.BatterySocAfterKwh >= battery.MinSocKwh - 1e-9);

            // Stored energy changes by exactly what flowed in or out, after losses.
            var expectedDelta = r.BatteryChargeKw * 0.25 * chargeEff - r.BatteryDischargeKw * 0.25 / dischargeEff;
            Assert.Equal(expectedDelta, r.BatterySocAfterKwh - r.BatterySocBeforeKwh, 9);
        }
    }

    [Fact]
    public void Invalid_battery_spec_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => (Battery with { CapacityKwh = 0 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Battery with { MinSocPercent = 100 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Battery with { ChargeEfficiency = 1.2 }).Validate());
    }
}
