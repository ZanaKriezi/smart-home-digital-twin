using SmartHomeTwin.Core.Alerts;
using SmartHomeTwin.Core.Energy;

namespace SmartHomeTwin.Tests;

public class KpiAndAlertTests
{
    private static readonly BatterySpec Battery = new(10, 5, 10);
    private static readonly TimeSpan Step = TimeSpan.FromMinutes(15);

    [Fact]
    public void Self_consumption_and_self_sufficiency_follow_their_definitions()
    {
        var kpis = new KpiAccumulator();
        // Step 1: 4 kW PV, 1 kW load, 3 kW to battery.  Step 2: 0 PV, 2 kW load, 2 kW from battery.  Step 3: 2 kW import.
        kpis.Add(new EnergyBalanceResult(1, 4, 1, 3, 0, 0, 0, 5, 5.75), Step);
        kpis.Add(new EnergyBalanceResult(2, 0, 0, 0, 2, 0, 0, 5.75, 5.25), Step);
        kpis.Add(new EnergyBalanceResult(2, 0, 0, 0, 0, 2, 0, 1, 1), Step);
        // Step 4: 3 kW PV, 1 kW load, 2 kW exported.
        kpis.Add(new EnergyBalanceResult(1, 3, 1, 0, 0, 0, 2, 10, 10), Step);

        var s = kpis.GetSummary();

        Assert.Equal(4, s.Steps);
        Assert.Equal(1.5, s.LoadKwh, 6);        // (1+2+2+1) × 0.25
        Assert.Equal(1.75, s.PvKwh, 6);         // (4+3) × 0.25
        Assert.Equal(0.5, s.GridImportKwh, 6);
        Assert.Equal(0.5, s.GridExportKwh, 6);
        Assert.Equal((1.75 - 0.5) / 1.75, s.SelfConsumptionRate!.Value, 6);
        Assert.Equal((1.5 - 0.5) / 1.5, s.SelfSufficiencyRate!.Value, 6);
        Assert.Equal(2, s.PeakLoadKw, 6);
        Assert.Equal(2, s.PeakGridImportKw, 6);
    }

    [Fact]
    public void Rates_are_null_without_solar_or_load()
    {
        var kpis = new KpiAccumulator();
        kpis.Add(new EnergyBalanceResult(0, 0, 0, 0, 0, 0, 0, 5, 5), Step);

        Assert.Null(kpis.GetSummary().SelfConsumptionRate);
        Assert.Null(kpis.GetSummary().SelfSufficiencyRate);
    }

    [Fact]
    public void Appliance_energy_is_summed_per_device()
    {
        var kpis = new KpiAccumulator();
        var zero = new EnergyBalanceResult(0, 0, 0, 0, 0, 0, 0, 5, 5);
        kpis.Add(zero, Step, new Dictionary<string, double> { ["oven"] = 0.6, ["kettle"] = 0.1 });
        kpis.Add(zero, Step, new Dictionary<string, double> { ["oven"] = 0.3 });

        Assert.Equal(0.9, kpis.GetSummary().ApplianceKwh["oven"], 6);
        Assert.Equal(0.1, kpis.GetSummary().ApplianceKwh["kettle"], 6);
    }

    [Fact]
    public void Alert_is_raised_once_when_a_condition_starts_and_cleared_when_it_ends()
    {
        var alerts = new AlertService(new AlertSettings { GridImportLimitKw = 3, HighLoadKw = 100 });
        var t = new DateTime(2026, 1, 14, 18, 0, 0);
        var high = new EnergyBalanceResult(5, 0, 0, 0, 1, 4, 0, 5, 4.75);
        var low = new EnergyBalanceResult(1, 0, 0, 0, 1, 0, 0, 4.75, 4.5);

        var first = alerts.Evaluate(high, Battery, t);
        var second = alerts.Evaluate(high, Battery, t.AddMinutes(15));
        Assert.Single(first);
        Assert.Equal(AlertType.HighGridImport, first[0].Type);
        Assert.Empty(second);
        Assert.Single(alerts.ActiveAlerts);

        alerts.Evaluate(low, Battery, t.AddMinutes(30));
        Assert.Empty(alerts.ActiveAlerts);

        Assert.Single(alerts.Evaluate(high, Battery, t.AddMinutes(45)));
    }

    [Fact]
    public void Battery_at_minimum_alert_is_raised_when_the_reserve_is_reached()
    {
        var alerts = new AlertService(new AlertSettings { GridImportLimitKw = 100, HighLoadKw = 100 });
        var t = new DateTime(2026, 1, 14, 19, 0, 0);

        var atMin = alerts.Evaluate(new EnergyBalanceResult(2, 0, 0, 0, 0, 2, 0, 1, 1), Battery, t);

        Assert.Contains(atMin, a => a.Type == AlertType.BatteryAtMinimum);
        Assert.Empty(alerts.Evaluate(new EnergyBalanceResult(0, 0, 0, 0, 0, 0, 0, 1, 1), Battery, t.AddMinutes(15)));
        alerts.Evaluate(new EnergyBalanceResult(0, 2, 0, 2, 0, 0, 0, 1, 1.5), Battery, t.AddMinutes(30));
        Assert.Empty(alerts.ActiveAlerts);
    }
}
