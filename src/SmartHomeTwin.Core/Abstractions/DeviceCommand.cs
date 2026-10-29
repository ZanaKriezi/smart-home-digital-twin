namespace SmartHomeTwin.Core.Abstractions;

public enum CommandAction
{
    On,
    Off
}

/// A user command for one device. It takes effect from the next simulation step.
public record DeviceCommand(string DeviceId, CommandAction Action);
