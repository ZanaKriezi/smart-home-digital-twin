namespace SmartHomeTwin.Core.Energy;

/// <summary>
/// Power flows of the home for one step, all in kW and all non-negative.
/// They satisfy PvKw + GridImportKw + BatteryDischargeKw = LoadKw + BatteryChargeKw + GridExportKw.
/// </summary>
public record EnergyBalanceResult(
    double LoadKw,
    double PvKw,
    double PvToLoadKw,
    double BatteryChargeKw,
    double BatteryDischargeKw,
    double GridImportKw,
    double GridExportKw,
    double BatterySocBeforeKwh,
    double BatterySocAfterKwh);
