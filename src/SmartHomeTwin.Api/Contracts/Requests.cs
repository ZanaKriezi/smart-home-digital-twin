using SmartHomeTwin.Core.Abstractions;

namespace SmartHomeTwin.Api.Contracts;

public record DeviceCommandRequest(CommandAction Action);

public record SpeedRequest(double SecondsPerTick);

public record SimulationStatus(
    bool IsRunning,
    double SecondsPerTick,
    int StepMinutes,
    DateTime CurrentTime,
    long TickIndex,
    string? ScenarioId,
    Guid? RunId,
    int PendingCommands);

public record ScenarioDto(
    string Id,
    string Name,
    string Description,
    int Seed,
    DateOnly StartDate,
    double InitialBatterySocPercent,
    bool IsActive);

public record ReadingDto(DateTime Timestamp, string DeviceId, double PowerKw, double EnergyKwh, bool IsOn);

public record EnergyPointDto(
    DateTime Timestamp,
    double LoadKw,
    double PvKw,
    double BatteryChargeKw,
    double BatteryDischargeKw,
    double BatterySocPercent,
    double GridImportKw,
    double GridExportKw);

public record AlertDto(DateTime Timestamp, string Type, string Message, double Value, double Threshold);
