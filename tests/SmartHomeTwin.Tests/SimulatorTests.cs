using SmartHomeTwin.Core.Abstractions;
using SmartHomeTwin.Core.Scenarios;
using SmartHomeTwin.Simulator;
using SmartHomeTwin.Simulator.Profiles;

namespace SmartHomeTwin.Tests;

public class SimulatorTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMinutes(15);

    private static SimulatedHome NewHome(ScenarioDefinition scenario)
    {
        var home = new SimulatedHome(TestSetup.LoadOptions(), TestSetup.LoadSimulatorConfig().ApplianceProfiles);
        home.Reset(scenario);
        return home;
    }

    private static ScenarioDefinition QuietScenario(params ApplianceUsage[] usages) => new()
    {
        Id = "test",
        Name = "Test",
        StartDate = new DateOnly(2026, 4, 15),
        Solar = new SolarSettings { PeakOutputKw = 0 },
        Usages = usages.ToList()
    };

    private static List<DeviceReading> RunDay(SimulatedHome home, DateOnly date)
    {
        var all = new List<DeviceReading>();
        for (var t = date.ToDateTime(TimeOnly.MinValue); t < date.AddDays(1).ToDateTime(TimeOnly.MinValue); t += Step)
            all.AddRange(home.ReadAll(t, Step));
        return all;
    }

    [Fact]
    public void Config_file_has_the_three_scenarios_and_a_profile_for_every_appliance()
    {
        var config = TestSetup.LoadSimulatorConfig();
        var options = TestSetup.LoadOptions();

        Assert.Equal(["normal", "high-consumption", "high-solar"], config.Scenarios.Select(s => s.Id));
        foreach (var device in options.Devices.Where(d => d.Type != "SolarPv"))
            Assert.Contains(config.ApplianceProfiles, p => p.DeviceId == device.Id);
        Assert.Equal(new TimeOnly(18, 0), config.Scenarios[1].Usages.First(u => u.DeviceId == "oven").Start);
    }

    [Fact]
    public void Every_device_gets_one_reading_per_step()
    {
        var home = NewHome(TestSetup.Scenario("normal"));
        var readings = home.ReadAll(new DateTime(2026, 4, 15, 12, 0, 0), Step);

        Assert.Equal(TestSetup.LoadOptions().Devices.Select(d => d.Id).Order(), readings.Select(r => r.DeviceId).Order());
        Assert.All(readings, r => Assert.Equal(new DateTime(2026, 4, 15, 12, 0, 0), r.Timestamp));
    }

    [Fact]
    public void Three_minute_kettle_run_uses_0_1_kWh_in_one_step()
    {
        var home = NewHome(QuietScenario(new ApplianceUsage { DeviceId = "kettle", Start = new TimeOnly(7, 0) }));
        var kettle = home.ReadAll(new DateTime(2026, 4, 15, 7, 0, 0), Step).Single(r => r.DeviceId == "kettle");

        Assert.Equal(0.4, kettle.AveragePowerKw, 6); // 2 kW × 3/15
        Assert.Equal(0.1, kettle.AveragePowerKw * Step.TotalHours, 6);
        Assert.True(kettle.IsOn);
    }

    [Fact]
    public void Program_energy_over_a_day_matches_the_profile()
    {
        var profiles = TestSetup.LoadSimulatorConfig().ApplianceProfiles;
        var home = NewHome(QuietScenario(
            new ApplianceUsage { DeviceId = "dishwasher", Start = new TimeOnly(20, 7) },
            new ApplianceUsage { DeviceId = "oven", Start = new TimeOnly(18, 0), DurationMinutes = 60 }));

        var day = RunDay(home, new DateOnly(2026, 4, 15));

        double Kwh(string id) => day.Where(r => r.DeviceId == id).Sum(r => r.AveragePowerKw * 0.25);
        var dishwasherExpected = profiles.Single(p => p.DeviceId == "dishwasher").Phases.Sum(p => p.Minutes * p.PowerKw) / 60;
        Assert.Equal(dishwasherExpected, Kwh("dishwasher"), 6);
        Assert.Equal((15 * 2.5 + 45 * 1.3) / 60, Kwh("oven"), 6); // last phase extended to 60 minutes
    }

    [Fact]
    public void Fridge_cycles_and_uses_about_its_duty_cycle()
    {
        var home = NewHome(QuietScenario());
        var fridge = RunDay(home, new DateOnly(2026, 4, 15)).Where(r => r.DeviceId == "fridge").ToList();

        var kwh = fridge.Sum(r => r.AveragePowerKw * 0.25);
        Assert.InRange(kwh, 0.15 * 24 * 0.33, 0.15 * 24 * 0.37);
        Assert.Contains(fridge, r => r.AveragePowerKw < 0.15); // not running the whole step
    }

    [Fact]
    public void No_solar_output_at_night_and_peak_near_noon()
    {
        var home = NewHome(TestSetup.Scenario("high-solar"));
        var solar = RunDay(home, new DateOnly(2026, 6, 17)).Where(r => r.DeviceId == "solar").ToList();

        Assert.All(solar.Where(r => r.Timestamp.Hour < 5 || r.Timestamp.Hour >= 21), r => Assert.Equal(0, r.AveragePowerKw));
        var peak = solar.MaxBy(r => r.AveragePowerKw)!;
        Assert.InRange(peak.Timestamp.Hour, 12, 14);
        Assert.InRange(peak.AveragePowerKw, 3.8, 4.2);
        Assert.InRange(solar.Sum(r => r.AveragePowerKw * 0.25), 30, 34); // ≈ 4.2 kW × 15.75 h / 2, minus clouds
    }

    [Fact]
    public void Same_seed_gives_identical_readings()
    {
        var a = RunDay(NewHome(TestSetup.Scenario("normal")), new DateOnly(2026, 4, 15));
        var b = RunDay(NewHome(TestSetup.Scenario("normal")), new DateOnly(2026, 4, 15));
        var c = RunDay(NewHome(TestSetup.Scenario("normal") with { Seed = 7 }), new DateOnly(2026, 4, 15));

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void Switching_off_stops_the_device_and_skips_planned_runs()
    {
        var home = NewHome(QuietScenario(new ApplianceUsage { DeviceId = "oven", Start = new TimeOnly(18, 0) }));
        var t = new DateTime(2026, 4, 15, 18, 0, 0);

        Assert.True(home.ReadAll(t, Step).Single(r => r.DeviceId == "oven").IsOn);
        home.SendCommand(new DeviceCommand("oven", CommandAction.Off));
        var after = home.ReadAll(t + Step, Step).Single(r => r.DeviceId == "oven");

        Assert.Equal(0, after.AveragePowerKw);
        Assert.False(after.IsOn);
    }

    [Fact]
    public void Switching_a_program_appliance_on_starts_a_run_in_the_next_step()
    {
        var home = NewHome(QuietScenario());
        var t = new DateTime(2026, 4, 15, 11, 0, 0);

        Assert.Equal(0, home.ReadAll(t, Step).Single(r => r.DeviceId == "washing-machine").AveragePowerKw);
        home.SendCommand(new DeviceCommand("washing-machine", CommandAction.On));
        var next = home.ReadAll(t + Step, Step).Single(r => r.DeviceId == "washing-machine");

        Assert.Equal(2.0, next.AveragePowerKw, 6);
        Assert.Contains(home.PlannedRuns, r => r.DeviceId == "washing-machine" && r.Start == t + Step);
    }

    [Fact]
    public void Switching_the_fridge_off_and_on_again()
    {
        var home = NewHome(QuietScenario());
        var t = new DateTime(2026, 4, 15, 0, 0, 0);
        home.SendCommand(new DeviceCommand("fridge", CommandAction.Off));
        var off = Enumerable.Range(0, 4).Sum(i => home.ReadAll(t.AddMinutes(15 * i), Step).Single(r => r.DeviceId == "fridge").AveragePowerKw);
        home.SendCommand(new DeviceCommand("fridge", CommandAction.On));
        var on = Enumerable.Range(4, 4).Sum(i => home.ReadAll(t.AddMinutes(15 * i), Step).Single(r => r.DeviceId == "fridge").AveragePowerKw);

        Assert.Equal(0, off);
        Assert.True(on > 0);
    }

    [Fact]
    public void Phases_are_cut_or_extended_to_the_requested_duration()
    {
        var oven = TestSetup.LoadSimulatorConfig().ApplianceProfiles.Single(p => p.DeviceId == "oven");

        Assert.Equal([new LoadPhase(15, 2.5), new LoadPhase(15, 1.3)], oven.PhasesFor(30));
        Assert.Equal([new LoadPhase(10, 2.5)], oven.PhasesFor(10));
        Assert.Equal(60, oven.PhasesFor(60).Sum(p => p.Minutes));
        Assert.Equal(45, oven.DefaultDurationMinutes);
    }

    [Fact]
    public void Missing_load_profile_is_reported_at_startup()
    {
        var options = TestSetup.LoadOptions();
        options.Devices.Add(new() { Id = "toaster", Name = "Toaster", Type = "Oven", RatedPowerKw = 1 });

        var ex = Assert.Throws<InvalidOperationException>(() => new SimulatedHome(options, TestSetup.LoadSimulatorConfig().ApplianceProfiles));
        Assert.Contains("toaster", ex.Message);
    }
}
