namespace SmartHomeTwin.Core.Abstractions;

/// One measurement for one device for one simulation step.
/// AveragePowerKw is the mean power over the step, so energy = AveragePowerKw * step length in hours.

public record DeviceReading(
    string DeviceId,
    DateTime Timestamp,
    double AveragePowerKw,
    bool IsOn);
