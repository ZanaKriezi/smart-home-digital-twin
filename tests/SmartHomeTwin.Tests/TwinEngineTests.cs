using SmartHomeTwin.Core.Abstractions;
using SmartHomeTwin.Core.Alerts;

namespace SmartHomeTwin.Tests;

public class TwinEngineTests
{
    private const int StepsPerDay = 96;

    [Fact]
    public void Tick_needs_a_started_scenario()
    {
        var (engine, _, _, _) = TestSetup.CreateEngine();
        Assert.Throws<InvalidOperationException>(() => engine.Tick());
    }

    [Fact]
    public void Each_tick_moves_the_clock_by_15_minutes_and_saves_the_step()
    {
        var (engine, _, store, scenarios) = TestSetup.CreateEngine();
        scenarios.Start("normal", autoRun: false);

        var first = engine.Tick();
        var second = engine.Tick();

        Assert.Equal(new DateTime(2026, 4, 15, 0, 0, 0), first.Timestamp);
        Assert.Equal(new DateTime(2026, 4, 15, 0, 15, 0), second.Timestamp);
        Assert.Equal(new DateTime(2026, 4, 15, 0, 30, 0), engine.Clock.CurrentTime);
        Assert.Equal(2, store.Ticks.Count);
        Assert.Single(store.Runs);
        Assert.All(first.Readings, r => Assert.Equal(first.Timestamp, r.Timestamp));
        Assert.Same(second.Snapshot, engine.LatestSnapshot);
    }

    [Fact]
    public void Home_twin_load_equals_sum_of_appliance_readings()
    {
        var (engine, _, _, scenarios) = TestSetup.CreateEngine();
        scenarios.Start("normal", autoRun: false);

        for (var i = 0; i < StepsPerDay; i++)
        {
            var tick = engine.Tick();
            var applianceSum = tick.Readings.Where(r => r.DeviceId != "solar").Sum(r => r.AveragePowerKw);
            Assert.Equal(applianceSum, tick.Balance.LoadKw, 9);
            Assert.Equal(tick.Readings.Single(r => r.DeviceId == "solar").AveragePowerKw, tick.Balance.PvKw, 9);
            Assert.Equal(tick.Balance.BatterySocAfterKwh, engine.Home.Battery.StateOfChargeKwh, 9);
        }
    }

    [Fact]
    public void Command_takes_effect_in_the_next_tick()
    {
        var (engine, _, _, scenarios) = TestSetup.CreateEngine();
        scenarios.Start("normal", autoRun: false);
        engine.Tick();

        engine.Commands.Submit(new DeviceCommand("oven", CommandAction.On));
        Assert.False(engine.Home.Find("oven")!.IsOn);
        var tick = engine.Tick();

        Assert.True(engine.Home.Find("oven")!.IsOn);
        Assert.Equal(2.5, tick.Snapshot.Devices.Single(d => d.Id == "oven").PowerKw, 6);

        engine.Commands.Submit(new DeviceCommand("oven", CommandAction.Off));
        engine.Tick();
        Assert.False(engine.Home.Find("oven")!.IsOn);
        Assert.False(engine.Home.Find("oven")!.IsEnabled);
    }

    [Fact]
    public void Normal_day_charges_the_battery_at_midday_and_uses_it_in_the_evening()
    {
        var (engine, _, store, scenarios) = TestSetup.CreateEngine();
        scenarios.Start("normal", autoRun: false);
        for (var i = 0; i < StepsPerDay; i++) engine.Tick();

        Assert.Contains(store.Ticks, t => t.Balance.BatteryChargeKw > 0 && t.Timestamp.Hour is >= 9 and < 17);
        Assert.Contains(store.Ticks, t => t.Balance.BatteryDischargeKw > 0 && t.Timestamp.Hour is >= 18 and < 22);

        // Day totals add up: what came in equals what went out plus the change in the battery.
        var kpis = engine.GetRunKpis();
        var socChange = store.Ticks[^1].Balance.BatterySocAfterKwh - store.Ticks[0].Balance.BatterySocBeforeKwh;
        Assert.Equal(kpis.PvKwh + kpis.GridImportKwh, kpis.LoadKwh + kpis.GridExportKwh + socChange, 6);
        Assert.Equal(kpis.LoadKwh, kpis.ApplianceKwh.Values.Sum(), 6);
    }

    [Fact]
    public void High_consumption_drains_battery_to_minimum_then_imports_and_raises_alerts()
    {
        var (engine, _, store, scenarios) = TestSetup.CreateEngine();
        scenarios.Start("high-consumption", autoRun: false);
        for (var i = 0; i < StepsPerDay; i++) engine.Tick();

        var peak = store.Ticks.Single(t => t.Timestamp == new DateTime(2026, 1, 14, 18, 0, 0));
        Assert.True(peak.Balance.LoadKw > 4.0, $"evening peak load was {peak.Balance.LoadKw:0.00} kW");

        var spec = engine.Home.Battery.Spec;
        var firstAtMin = store.Ticks.First(t => t.Balance.BatterySocAfterKwh <= spec.MinSocKwh + 1e-9);
        Assert.Contains(store.Ticks, t => t.TickIndex > firstAtMin.TickIndex && t.Balance.GridImportKw > 0);
        Assert.All(store.Ticks, t => Assert.True(t.Balance.BatterySocAfterKwh >= spec.MinSocKwh - 1e-9));

        var alertTypes = store.Ticks.SelectMany(t => t.NewAlerts).Select(a => a.Type).ToHashSet();
        Assert.Contains(AlertType.BatteryAtMinimum, alertTypes);
        Assert.Contains(AlertType.HighLoad, alertTypes);
    }

    [Fact]
    public void High_solar_charges_battery_first_and_then_exports()
    {
        var (engine, _, store, scenarios) = TestSetup.CreateEngine();
        scenarios.Start("high-solar", autoRun: false);
        for (var i = 0; i < StepsPerDay; i++) engine.Tick();

        var firstCharge = store.Ticks.First(t => t.Balance.BatteryChargeKw > 0);
        var firstExport = store.Ticks.First(t => t.Balance.GridExportKw > 0);
        Assert.True(firstCharge.TickIndex <= firstExport.TickIndex);
        Assert.Contains(store.Ticks, t => t.Balance.BatterySocAfterKwh >= engine.Home.Battery.Spec.CapacityKwh - 1e-9);
        Assert.True(engine.GetRunKpis().GridExportKwh > 10);
    }

    [Fact]
    public void Same_scenario_and_seed_reproduce_the_same_run()
    {
        List<double> Run()
        {
            var (engine, _, store, scenarios) = TestSetup.CreateEngine();
            scenarios.Start("normal", autoRun: false);
            for (var i = 0; i < StepsPerDay * 2; i++) engine.Tick();
            return store.Ticks.SelectMany(t => new[] { t.Balance.LoadKw, t.Balance.PvKw, t.Balance.BatterySocAfterKwh, t.Balance.GridImportKw }).ToList();
        }

        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void Starting_a_scenario_resets_the_twin()
    {
        var (engine, _, store, scenarios) = TestSetup.CreateEngine();
        scenarios.Start("high-solar", autoRun: false);
        for (var i = 0; i < 50; i++) engine.Tick();
        engine.Commands.Submit(new DeviceCommand("fridge", CommandAction.Off));
        engine.Tick();

        var run = scenarios.Start("high-consumption", autoRun: true);

        Assert.True(engine.IsRunning);
        Assert.Equal(new DateTime(2026, 1, 14), engine.Clock.CurrentTime);
        Assert.Equal(30, engine.Home.Battery.StateOfChargePercent, 6);
        Assert.True(engine.Home.Find("fridge")!.IsEnabled);
        Assert.Equal(0, engine.GetRunKpis().Steps);
        Assert.Equal(run.Id, engine.LatestSnapshot!.RunId);
        Assert.Equal(2, store.Runs.Count);
    }

    [Fact]
    public void Unknown_scenario_is_rejected()
    {
        var (_, _, _, scenarios) = TestSetup.CreateEngine();
        Assert.Throws<KeyNotFoundException>(() => scenarios.Start("storm"));
    }
}
