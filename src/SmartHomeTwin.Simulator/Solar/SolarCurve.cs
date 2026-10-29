using SmartHomeTwin.Core.Scenarios;

namespace SmartHomeTwin.Simulator.Solar;

/// <summary>
/// Solar output of a clear day: zero before sunrise and after sunset, and the scenario's peak output at noon.
/// The curve is a squared sine, which keeps the output low while the sun is low. Its daily energy is
/// peak × day length / 2, for example 4.2 kW × 15.75 h / 2 ≈ 33 kWh on a sunny June day for 5 kWp.
/// </summary>
public class SolarCurve
{
    private readonly SolarSettings _settings;

    public SolarCurve(SolarSettings settings)
    {
        if (settings.SunsetHour <= settings.SunriseHour)
            throw new ArgumentException("Sunset must be after sunrise.", nameof(settings));
        if (settings.PeakOutputKw < 0)
            throw new ArgumentOutOfRangeException(nameof(settings), "Peak output cannot be negative.");
        _settings = settings;
    }

    /// <summary>Solar output in kW at the given moment.</summary>
    public double PowerKwAt(DateTime time)
    {
        double hour = time.TimeOfDay.TotalHours;
        if (hour <= _settings.SunriseHour || hour >= _settings.SunsetHour)
            return 0;

        // 0 at sunrise, 0.5 at noon, 1 at sunset
        double dayFraction = (hour - _settings.SunriseHour) / (_settings.SunsetHour - _settings.SunriseHour);
        double sine = Math.Sin(Math.PI * dayFraction);
        return _settings.PeakOutputKw * sine * sine;
    }
}
