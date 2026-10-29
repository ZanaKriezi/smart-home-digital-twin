using Microsoft.EntityFrameworkCore;
using SmartHomeTwin.Api.Data;
using SmartHomeTwin.Core;
using SmartHomeTwin.Core.Scenarios;
using SmartHomeTwin.Core.Simulation;

namespace SmartHomeTwin.Api.Services;

/// <summary>
/// Runs the twin loop in the background while the API is running:
/// prepares the database, starts the default scenario and then calls Tick() every few seconds.
/// </summary>
public class TwinHostedService : BackgroundService
{
    private readonly TwinEngine _engine;
    private readonly ScenarioManager _scenarios;
    private readonly IDbContextFactory<TwinDbContext> _dbFactory;
    private readonly TwinOptions _options;
    private readonly ILogger<TwinHostedService> _logger;

    public TwinHostedService(TwinEngine engine, ScenarioManager scenarios, IDbContextFactory<TwinDbContext> dbFactory,
        TwinOptions options, ILogger<TwinHostedService> logger)
    {
        _engine = engine;
        _scenarios = scenarios;
        _dbFactory = dbFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create or update the database tables, and write the devices and scenarios into them.
        using (TwinDbContext db = _dbFactory.CreateDbContext())
        {
            db.Database.Migrate();
            ReferenceDataSeeder.Seed(db, _engine.Home, _scenarios.All);
        }

        _scenarios.Start(_options.DefaultScenarioId);
        _logger.LogInformation("Started scenario {Scenario}", _options.DefaultScenarioId);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_engine.IsRunning)
            {
                try
                {
                    _engine.Tick();
                }
                catch (Exception ex)
                {
                    _engine.Pause();
                    _logger.LogError(ex, "A simulation step failed; the simulation was paused.");
                }
            }

            // Wait until the next step (or check again shortly when paused).
            double seconds = _engine.IsRunning ? _engine.SecondsPerTick : 0.2;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
