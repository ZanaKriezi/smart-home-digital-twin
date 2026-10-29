namespace SmartHomeTwin.Core.Devices;

/// An electrical consumer in the home.
public abstract class ApplianceTwin : DeviceTwin
{
    protected ApplianceTwin(string id, string name, double ratedPowerKw) : base(id, name, ratedPowerKw)
    {
    }

    public override DeviceCategory Category => DeviceCategory.Consumer;

    /// True for appliances whose start time can be moved (dishwasher, washing machine).
    public virtual bool IsShiftable => false;
}

public class FridgeTwin : ApplianceTwin
{
    public FridgeTwin(string id, string name, double ratedPowerKw) : base(id, name, ratedPowerKw) { }
    public override string Type => "Fridge";
}

public class OvenTwin : ApplianceTwin
{
    public OvenTwin(string id, string name, double ratedPowerKw) : base(id, name, ratedPowerKw) { }
    public override string Type => "Oven";
}

public class KettleTwin : ApplianceTwin
{
    public KettleTwin(string id, string name, double ratedPowerKw) : base(id, name, ratedPowerKw) { }
    public override string Type => "Kettle";
}

public class DishwasherTwin : ApplianceTwin
{
    public DishwasherTwin(string id, string name, double ratedPowerKw) : base(id, name, ratedPowerKw) { }
    public override string Type => "Dishwasher";
    public override bool IsShiftable => true;
}

public class WashingMachineTwin : ApplianceTwin
{
    public WashingMachineTwin(string id, string name, double ratedPowerKw) : base(id, name, ratedPowerKw) { }
    public override string Type => "WashingMachine";
    public override bool IsShiftable => true;
}
