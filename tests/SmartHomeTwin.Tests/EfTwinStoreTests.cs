using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmartHomeTwin.Api.Data;
using SmartHomeTwin.Core;
using SmartHomeTwin.Core.Devices;
using SmartHomeTwin.Core.Scenarios;
using SmartHomeTwin.Core.Simulation;
using SmartHomeTwin.Simulator;

namespace SmartHomeTwin.Tests;

/// <summary>
/// Checks the EF Core mapping with an in-memory SQLite database. The application itself uses SQL Server;
/// the same model is used for both, only the provider differs.
/// </summary>
public sealed class EfTwinStoreTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<TwinDbContext> _options;

    public EfTwinStoreTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<TwinDbContext>().UseSqlite(_connection).Options;
        using var db = new TwinDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private sealed class Factory(DbContextOptions<TwinDbContext> options) : IDbContextFactory<TwinDbContext>
    {
        public TwinDbContext CreateDbContext() => new(options);
    }

    [Fact]
    public void One_day_is_stored_with_a_snapshot_per_step_and_a_reading_per_device()
    {
        TwinOptions options = TestSetup.LoadOptions();
        var simConfig = TestSetup.LoadSimulatorConfig();
        var home = HomeTwinFactory.Create(options);
        var store = new EfTwinStore(new Factory(_options));
        var engine = new TwinEngine(options, home, new SimulatedHome(options, simConfig.ApplianceProfiles), store, new CommandHandler());
        var scenarios = new ScenarioManager(simConfig.Scenarios, engine);

        using (var db = new TwinDbContext(_options))
            ReferenceDataSeeder.Seed(db, home, scenarios.All);

        var run = scenarios.Start("high-consumption", autoRun: false);
        for (var i = 0; i < 96; i++) engine.Tick();

        using var check = new TwinDbContext(_options);
        Assert.Equal(7, check.Devices.Count());
        Assert.Equal(3, check.Scenarios.Count());
        Assert.Equal(96, check.HomeSnapshots.Count(s => s.RunId == run.Id));
        Assert.Equal(96 * options.Devices.Count, check.Readings.Count(r => r.RunId == run.Id));
        Assert.True(check.Alerts.Any(a => a.RunId == run.Id));

        var kpis = engine.GetRunKpis();
        var storedLoadKwh = check.HomeSnapshots.Where(s => s.RunId == run.Id).Select(s => s.LoadKw).ToList().Sum() * 0.25;
        Assert.Equal(kpis.LoadKwh, storedLoadKwh, 6);
    }

    [Fact]
    public void Saving_the_same_step_twice_fails_and_stores_nothing_of_the_second_attempt()
    {
        var options = TestSetup.LoadOptions();
        var simConfig = TestSetup.LoadSimulatorConfig();
        var home = HomeTwinFactory.Create(options);
        var recording = new RecordingStore();
        var engine = new TwinEngine(options, home, new SimulatedHome(options, simConfig.ApplianceProfiles), recording, new CommandHandler());
        using (var db = new TwinDbContext(_options))
            ReferenceDataSeeder.Seed(db, home, simConfig.Scenarios);

        var run = engine.StartScenario(simConfig.Scenarios[0]);
        var tick = engine.Tick();
        var store = new EfTwinStore(new Factory(_options));
        store.BeginRun(run);
        store.SaveTick(tick);

        Assert.ThrowsAny<DbUpdateException>(() => store.SaveTick(tick));

        using var check = new TwinDbContext(_options);
        Assert.Equal(1, check.HomeSnapshots.Count());
        Assert.Equal(options.Devices.Count, check.Readings.Count());
    }
}
