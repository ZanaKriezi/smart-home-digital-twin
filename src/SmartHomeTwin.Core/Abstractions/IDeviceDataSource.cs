using SmartHomeTwin.Core.Scenarios;

namespace SmartHomeTwin.Core.Abstractions;

/// The only connection between the twin core and the (simulated) home.
/// The simulator implements it today; an MQTT client or a smart-meter adapter could implement it later
/// without changes to the twin core, the API or the dashboard.

public interface IDeviceDataSource
{
    ///Returns one reading per device for the step that starts at stepStart.
    List<DeviceReading> ReadAll(DateTime stepStart, TimeSpan stepLength);

    ///Passes a user command to the home. It affects the next call to ReadAll.
    void SendCommand(DeviceCommand command);

    ///Prepares the data source for a new scenario run. A real data source can ignore this.
    void Reset(ScenarioDefinition scenario);
}
