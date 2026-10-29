using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SmartHomeTwin.Api.Data;
using SmartHomeTwin.Api.Services;
using SmartHomeTwin.Core;
using SmartHomeTwin.Core.Abstractions;
using SmartHomeTwin.Core.Devices;
using SmartHomeTwin.Core.Scenarios;
using SmartHomeTwin.Core.Simulation;
using SmartHomeTwin.Simulator;

var builder = WebApplication.CreateBuilder(args);

// Configuration: device list, battery and alert limits from appsettings.json; profiles and scenarios from scenarios.json.
TwinOptions twinOptions = builder.Configuration.GetSection(TwinOptions.SectionName).Get<TwinOptions>()
                  ?? throw new InvalidOperationException("Missing 'Twin' section in appsettings.json.");
SimulatorConfig simulatorConfig = SimulatorConfig.Load(Path.Combine(builder.Environment.ContentRootPath, "scenarios.json"));

// Database (SQL Server through EF Core).
builder.Services.AddDbContextFactory<TwinDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("TwinDatabase")));

// Simulator -> IDeviceDataSource -> twin core. Replacing the simulator means registering another IDeviceDataSource here.
builder.Services.AddSingleton(twinOptions);
builder.Services.AddSingleton<IDeviceDataSource>(new SimulatedHome(twinOptions, simulatorConfig.ApplianceProfiles));
builder.Services.AddSingleton(HomeTwinFactory.Create(twinOptions));
builder.Services.AddSingleton<ITwinStore, EfTwinStore>();
builder.Services.AddSingleton<CommandHandler>();
builder.Services.AddSingleton<TwinEngine>();
builder.Services.AddSingleton(services => new ScenarioManager(simulatorConfig.Scenarios, services.GetRequiredService<TwinEngine>()));
builder.Services.AddHostedService<TwinHostedService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// The dashboard's development server runs on another port.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

app.Run();
