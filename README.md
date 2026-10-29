# Smart Home Digital Twin – prototype

Software-based digital twin prototype of a simulated smart home (diploma thesis).
A C# simulator generates device data every 15 simulated minutes, the twin core updates the device twins and the
energy balance of the home, every step is stored in SQL Server, and a React dashboard shows and controls the twin.

## Layout

| Folder | Layer | Contents |
|---|---|---|
| `src/SmartHomeTwin.Core` | Digital twin core | `DeviceTwin` base class and device twins, `HomeTwin`, `EnergyBalanceCalculator`, `KpiAccumulator`, `AlertService`, `SimulationClock`, `TwinEngine` (tick loop), `ScenarioManager`, `CommandHandler`, `IDeviceDataSource` |
| `src/SmartHomeTwin.Simulator` | Data simulator | `SimulatedHome` (implements `IDeviceDataSource`), synthetic appliance load profiles, solar curve |
| `src/SmartHomeTwin.Api` | Web API + database | ASP.NET Core controllers, EF Core `TwinDbContext` and migrations, `TwinHostedService` (runs the loop), `appsettings.json`, `scenarios.json` |
| `dashboard` | Web dashboard | React + TypeScript + Recharts, four pages: overview, kitchen appliances, energy flow, scenarios |
| `tests/SmartHomeTwin.Tests` | Tests | xUnit tests for the energy balance, indicators, alerts, simulator, tick loop and database mapping |

## Requirements

- .NET 8 SDK
- SQL Server: LocalDB (installed with Visual Studio) works with the default connection string; SQL Server Express or Docker also work
- Node.js 18 or newer

## Run

1. API (creates the database on first start and starts the "normal" scenario):

   ```
   dotnet run --project src/SmartHomeTwin.Api
   ```

   It listens on http://localhost:5080; the API documentation is at http://localhost:5080/swagger.
   To use another SQL Server, change `ConnectionStrings:TwinDatabase` in `src/SmartHomeTwin.Api/appsettings.json`,
   for example `Server=localhost,1433;Database=SmartHomeTwin;User Id=sa;Password=...;TrustServerCertificate=True`.

2. Dashboard, in a second terminal:

   ```
   cd dashboard
   npm install
   npm run dev
   ```

   Open http://localhost:5173.

3. Tests:

   ```
   dotnet test
   ```

## Configuration

- `appsettings.json` → `Twin`: step length (15 min), real seconds per step, devices and rated power,
  battery (10 kWh, 5 kW, 10 % reserve, efficiencies), alert thresholds.
- `scenarios.json`: appliance load profiles (program phases, fridge cycling) and the three scenarios
  (date, seed, initial battery level, solar curve, planned appliance uses).

## REST API

| Method | Path | Purpose |
|---|---|---|
| GET | `/api/devices`, `/api/devices/{id}` | Device twins and their current state |
| POST | `/api/devices/{id}/commands` `{"action":"On"\|"Off"}` | Switch a device; applies from the next step |
| GET | `/api/energy/current` | Current home state (power flows, battery, devices, active alerts) |
| GET | `/api/energy/history?from&to` | Energy balance per step |
| GET | `/api/energy/kpis?from&to` | Indicators (kWh per appliance, self-consumption, self-sufficiency, import/export) |
| GET | `/api/readings?deviceId&from&to` | Stored device readings |
| GET | `/api/alerts` | Alerts of the current run |
| GET/POST | `/api/scenarios`, `/api/scenarios/{id}/start` | List and start scenarios |
| GET/POST/PUT | `/api/simulation`, `/start`, `/pause`, `/step`, `/speed` | Control the simulation |

Times (`from`, `to`, timestamps) are simulated time. History endpoints default to the current scenario run.

## The twin loop (one tick = 15 simulated minutes)

1. Apply the commands sent since the last tick.
2. `IDeviceDataSource.ReadAll` returns one reading per device (average power over the step).
3. The device twins take over the readings.
4. Energy balance: solar covers the load first; surplus charges the battery, then is exported;
   deficit is covered by the battery down to its reserve, then imported. Indicators and alerts are updated.
5. Readings, home snapshot and new alerts are saved in one transaction; the clock moves on.
