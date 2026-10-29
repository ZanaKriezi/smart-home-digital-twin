namespace SmartHomeTwin.Core.Energy;

/// <summary>
/// Resolves the energy balance of the home for one step with a fixed priority order:
/// solar output first covers the load; a surplus charges the battery and the rest is exported;
/// a deficit is covered first by the battery and then by grid import.
/// </summary>
public static class EnergyBalanceCalculator
{
    public static EnergyBalanceResult Calculate(
        double loadKw,
        double pvKw,
        BatterySpec battery,
        double socKwh,
        TimeSpan stepLength,
        bool batteryEnabled = true)
    {
        if (stepLength <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(stepLength));
        if (double.IsNaN(loadKw) || double.IsNaN(pvKw)) throw new ArgumentException("Power values must be numbers.");

        loadKw = Math.Max(0, loadKw);
        pvKw = Math.Max(0, pvKw);
        socKwh = Math.Clamp(socKwh, 0, battery.CapacityKwh);
        var hours = stepLength.TotalHours;

        var pvToLoad = Math.Min(pvKw, loadKw);
        var surplus = pvKw - pvToLoad;
        var deficit = loadKw - pvToLoad;

        double charge = 0, discharge = 0;
        var socAfter = socKwh;

        if (batteryEnabled && surplus > 0)
        {
            // Power that can still be stored before the battery is full.
            var roomKw = (battery.CapacityKwh - socKwh) / (hours * battery.ChargeEfficiency);
            charge = Math.Max(0, Math.Min(surplus, Math.Min(battery.MaxPowerKw, roomKw)));
            socAfter = socKwh + charge * hours * battery.ChargeEfficiency;
        }
        else if (batteryEnabled && deficit > 0)
        {
            // Power that can still be delivered before the battery reaches its minimum state of charge.
            var availableKw = Math.Max(0, socKwh - battery.MinSocKwh) * battery.DischargeEfficiency / hours;
            discharge = Math.Max(0, Math.Min(deficit, Math.Min(battery.MaxPowerKw, availableKw)));
            socAfter = socKwh - discharge * hours / battery.DischargeEfficiency;
        }

        var export = surplus - charge;
        var import = deficit - discharge;

        return new EnergyBalanceResult(
            LoadKw: loadKw,
            PvKw: pvKw,
            PvToLoadKw: pvToLoad,
            BatteryChargeKw: charge,
            BatteryDischargeKw: discharge,
            GridImportKw: import,
            GridExportKw: export,
            BatterySocBeforeKwh: socKwh,
            BatterySocAfterKwh: Math.Clamp(socAfter, 0, battery.CapacityKwh));
    }
}
