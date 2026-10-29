using SmartHomeTwin.Core.Abstractions;

namespace SmartHomeTwin.Core.Simulation;

/// Collects user commands until the start of the next tick.
public class CommandHandler
{
    private readonly object _sync = new object();
    private readonly List<DeviceCommand> _pending = new List<DeviceCommand>();

    public void Submit(DeviceCommand command)
    {
        lock (_sync)
        {
            _pending.Add(command);
        }
    }

    public int PendingCount
    {
        get
        {
            lock (_sync)
            {
                return _pending.Count;
            }
        }
    }

    /// Returns all waiting commands and empties the list.
    public List<DeviceCommand> TakeAll()
    {
        lock (_sync)
        {
            var commands = _pending.ToList();
            _pending.Clear();
            return commands;
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _pending.Clear();
        }
    }
}
