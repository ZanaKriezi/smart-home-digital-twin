namespace SmartHomeTwin.Core.Energy;

/// <summary>Energy indicators over a period (a scenario run, a day, or a selected time range).</summary>
public record KpiSummary(
    int Steps,
    double LoadKwh,
    double PvKwh,
    double GridImportKwh,
    double GridExportKwh,
    double BatteryChargeKwh,
    double BatteryDischargeKwh,
    double PeakLoadKw,
    double PeakGridImportKw,
    // Share of the solar energy used in the home or stored instead of exported. Null without solar energy.
    double? SelfConsumptionRate,
    // Share of the consumption covered by own solar energy, directly or through the battery. Null without load.
    double? SelfSufficiencyRate,
    Dictionary<string, double> ApplianceKwh);

/// <summary>Adds up the results of many steps into a KpiSummary.</summary>
public class KpiAccumulator
{
    private readonly Dictionary<string, double> _applianceKwh = new Dictionary<string, double>();
    private int _steps;
    private double _load, _pv, _import, _export, _charge, _discharge, _peakLoad, _peakImport;

    public void Add(EnergyBalanceResult balance, TimeSpan stepLength, Dictionary<string, double>? applianceEnergyKwh = null)
    {
        double h = stepLength.TotalHours;
        _steps++;
        _load += balance.LoadKw * h;
        _pv += balance.PvKw * h;
        _import += balance.GridImportKw * h;
        _export += balance.GridExportKw * h;
        _charge += balance.BatteryChargeKw * h;
        _discharge += balance.BatteryDischargeKw * h;
        _peakLoad = Math.Max(_peakLoad, balance.LoadKw);
        _peakImport = Math.Max(_peakImport, balance.GridImportKw);

        if (applianceEnergyKwh != null)
            AddApplianceEnergy(applianceEnergyKwh);
    }

    public void AddApplianceEnergy(Dictionary<string, double> applianceEnergyKwh)
    {
        foreach (KeyValuePair<string, double> item in applianceEnergyKwh)
            _applianceKwh[item.Key] = _applianceKwh.GetValueOrDefault(item.Key) + item.Value;
    }

    public KpiSummary GetSummary() => new KpiSummary(
        Steps: _steps,
        LoadKwh: _load,
        PvKwh: _pv,
        GridImportKwh: _import,
        GridExportKwh: _export,
        BatteryChargeKwh: _charge,
        BatteryDischargeKwh: _discharge,
        PeakLoadKw: _peakLoad,
        PeakGridImportKw: _peakImport,
        SelfConsumptionRate: _pv > 0 ? Math.Clamp((_pv - _export) / _pv, 0, 1) : null,
        SelfSufficiencyRate: _load > 0 ? Math.Clamp((_load - _import) / _load, 0, 1) : null,
        ApplianceKwh: new Dictionary<string, double>(_applianceKwh));

    public void Reset()
    {
        _applianceKwh.Clear();
        _steps = 0;
        _load = _pv = _import = _export = _charge = _discharge = _peakLoad = _peakImport = 0;
    }
}
