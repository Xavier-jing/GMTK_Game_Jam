/// <summary>
/// Stores temporary world progress for the current run.
/// Every value is cleared when a new run begins.
/// </summary>
public sealed class RunState
{
    public bool WallRepaired { get; private set; }

    public bool SteeringWheelRaised { get; private set; }

    public bool FridgeUnplugged { get; private set; }

    public bool BedConnected { get; private set; }

    public bool FabricPrepared { get; private set; }

    public bool RopeAttached { get; private set; }

    public bool ParachuteAnchored { get; private set; }

    public void MarkWallRepaired()
    {
        WallRepaired = true;
    }

    public void MarkSteeringWheelRaised()
    {
        SteeringWheelRaised = true;
    }

    public void MarkFridgeUnplugged()
    {
        FridgeUnplugged = true;
    }

    public void MarkBedConnected()
    {
        BedConnected = true;
    }

    public void MarkFabricPrepared()
    {
        FabricPrepared = true;
    }

    public void MarkRopeAttached()
    {
        RopeAttached = true;
    }

    public void MarkParachuteAnchored()
    {
        ParachuteAnchored = true;
    }

    public void Reset()
    {
        WallRepaired = false;
        SteeringWheelRaised = false;
        FridgeUnplugged = false;
        BedConnected = false;
        FabricPrepared = false;
        RopeAttached = false;
        ParachuteAnchored = false;
    }
}
