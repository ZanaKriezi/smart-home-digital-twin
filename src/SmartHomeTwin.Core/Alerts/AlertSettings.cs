namespace SmartHomeTwin.Core.Alerts;

/// Alert thresholds. The defaults are prototype values and can be changed in appsettings.json.
public class AlertSettings
{
    public double GridImportLimitKw { get; set; } = 3.0;
    public double HighLoadKw { get; set; } = 4.0;
}
