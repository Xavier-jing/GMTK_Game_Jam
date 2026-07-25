using System;

/// <summary>
/// Stores temporary world progress for the current run.
/// Every value is cleared when a new run begins.
/// </summary>
public sealed class RunState
{
    public event Action Changed;

    public bool DresserOpened { get; private set; }

    public bool WallRepaired { get; private set; }

    public bool SteeringWheelRaised { get; private set; }

    public bool FridgeUnplugged { get; private set; }

    public bool BedConnected { get; private set; }

    public bool FabricPrepared { get; private set; }

    public bool BedSwitchTriggered { get; private set; }

    public bool RopeAttached { get; private set; }

    public bool ParachuteAnchored { get; private set; }

    public void MarkDresserOpened()
    {
        SetFlag(RunFlagId.DresserOpened, true);
    }

    public void MarkWallRepaired()
    {
        SetFlag(RunFlagId.WallRepaired, true);
    }

    public void MarkSteeringWheelRaised()
    {
        SetFlag(RunFlagId.SteeringWheelRaised, true);
    }

    public void MarkFridgeUnplugged()
    {
        SetFlag(RunFlagId.FridgeUnplugged, true);
    }

    public void MarkBedConnected()
    {
        SetFlag(RunFlagId.BedConnected, true);
    }

    public void MarkFabricPrepared()
    {
        SetFlag(RunFlagId.FabricPrepared, true);
    }

    public void MarkBedSwitchTriggered()
    {
        SetFlag(RunFlagId.BedSwitchTriggered, true);
    }

    public void MarkRopeAttached()
    {
        SetFlag(RunFlagId.RopeAttached, true);
    }

    public void MarkParachuteAnchored()
    {
        SetFlag(RunFlagId.ParachuteAnchored, true);
    }

    public void Reset()
    {
        DresserOpened = false;
        WallRepaired = false;
        SteeringWheelRaised = false;
        FridgeUnplugged = false;
        BedConnected = false;
        FabricPrepared = false;
        BedSwitchTriggered = false;
        RopeAttached = false;
        ParachuteAnchored = false;
        Changed?.Invoke();
    }

    public bool GetFlag(RunFlagId flag)
    {
        switch (flag)
        {
            case RunFlagId.DresserOpened:
                return DresserOpened;
            case RunFlagId.WallRepaired:
                return WallRepaired;
            case RunFlagId.SteeringWheelRaised:
                return SteeringWheelRaised;
            case RunFlagId.FridgeUnplugged:
                return FridgeUnplugged;
            case RunFlagId.BedConnected:
                return BedConnected;
            case RunFlagId.FabricPrepared:
                return FabricPrepared;
            case RunFlagId.BedSwitchTriggered:
                return BedSwitchTriggered;
            case RunFlagId.RopeAttached:
                return RopeAttached;
            case RunFlagId.ParachuteAnchored:
                return ParachuteAnchored;
            default:
                return false;
        }
    }

    public void SetFlag(RunFlagId flag, bool value)
    {
        if (GetFlag(flag) == value)
        {
            return;
        }

        switch (flag)
        {
            case RunFlagId.DresserOpened:
                DresserOpened = value;
                break;
            case RunFlagId.WallRepaired:
                WallRepaired = value;
                break;
            case RunFlagId.SteeringWheelRaised:
                SteeringWheelRaised = value;
                break;
            case RunFlagId.FridgeUnplugged:
                FridgeUnplugged = value;
                break;
            case RunFlagId.BedConnected:
                BedConnected = value;
                break;
            case RunFlagId.FabricPrepared:
                FabricPrepared = value;
                break;
            case RunFlagId.BedSwitchTriggered:
                BedSwitchTriggered = value;
                break;
            case RunFlagId.RopeAttached:
                RopeAttached = value;
                break;
            case RunFlagId.ParachuteAnchored:
                ParachuteAnchored = value;
                break;
        }

        Changed?.Invoke();
    }
}
