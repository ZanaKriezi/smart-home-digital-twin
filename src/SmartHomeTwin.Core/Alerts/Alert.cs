namespace SmartHomeTwin.Core.Alerts;

public enum AlertType
{
    HighGridImport,
    HighLoad,
    BatteryAtMinimum
}

public record Alert(AlertType Type, DateTime Timestamp, string Message, double Value, double Threshold);
