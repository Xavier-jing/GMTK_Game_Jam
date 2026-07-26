public enum WorldPropId
{
    None = 0,
    Dresser = 1,
    CableBed = 2,
    TeaSet = 3,
    Vase = 4,
    Plank = 5,
    Refrigerator = 6,
    SmallWallHole = 7,
    Scissors = 8,
    Wrench = 9,
    LargeWallHole = 10,
    BedBlanket = 11,
    BedSwitch = 12,
    SteeringWheel = 13,
    PowerConnector = 14,
    Rope = 15
}

public enum WorldPropCommand
{
    Inspect,
    OpenDresser,
    TakeIntoCarrySlot,
    DropFromCarrySlot,
    AcquireInventoryItem,
    FirstWallStrike,
    SecondWallStrike,
    CutBlanket,
    TriggerBedSwitch,
    StartSteeringWheel,
    UnplugRefrigerator,
    ConnectBedPower,
    InstallPlank,
    AttachRopeToFabric,
    AnchorParachute,
    DeployParachute,
    RevealBedSwitch,
    AcquireBedRope,
    AttachRopeToPlayer
}

public enum RunFlagId
{
    DresserOpened,
    WallRepaired,
    SteeringWheelRaised,
    FridgeUnplugged,
    BedConnected,
    FabricPrepared,
    BedSwitchTriggered,
    RopeAttached,
    ParachuteAnchored,
    BedLifted,
    RopeTaken
}

public enum LoopProgressFlag
{
    TruthKnown,
    EndingTwoReached,
    EndingThreeReached
}

public static class WorldPropRules
{
    public static bool IsCarryable(WorldPropId propId)
    {
        return propId == WorldPropId.Dresser ||
               propId == WorldPropId.CableBed ||
               propId == WorldPropId.TeaSet ||
               propId == WorldPropId.Vase ||
               propId == WorldPropId.Plank ||
               propId == WorldPropId.Refrigerator;
    }

    public static bool IsInventoryItem(WorldPropId propId)
    {
        return propId == WorldPropId.Scissors ||
               propId == WorldPropId.Wrench ||
               propId == WorldPropId.Rope;
    }

    public static PlayerSlotItemKind GetSlotItemKind(WorldPropId propId)
    {
        switch (propId)
        {
            case WorldPropId.Dresser:
                return PlayerSlotItemKind.Dresser;
            case WorldPropId.CableBed:
                return PlayerSlotItemKind.CableBed;
            case WorldPropId.TeaSet:
                return PlayerSlotItemKind.TeaSet;
            case WorldPropId.Vase:
                return PlayerSlotItemKind.Vase;
            case WorldPropId.Plank:
                return PlayerSlotItemKind.Plank;
            case WorldPropId.Refrigerator:
                return PlayerSlotItemKind.Refrigerator;
            default:
                return PlayerSlotItemKind.None;
        }
    }

    public static bool IsPresent(
        WorldPropId propId,
        LoopProgress loopProgress,
        RunState runState)
    {
        if (loopProgress == null || runState == null)
        {
            return true;
        }

        switch (propId)
        {
            case WorldPropId.Scissors:
                return runState.DresserOpened;
            case WorldPropId.Wrench:
                return runState.DresserOpened;
            case WorldPropId.SmallWallHole:
                return !runState.WallStruckOnce;
            case WorldPropId.LargeWallHole:
                return runState.WallStruckOnce;
            case WorldPropId.BedBlanket:
                return loopProgress.TruthKnown;
            case WorldPropId.BedSwitch:
                return loopProgress.TruthKnown && runState.BedLifted;
            case WorldPropId.SteeringWheel:
                return loopProgress.TruthKnown && runState.BedSwitchTriggered;
            case WorldPropId.PowerConnector:
                return loopProgress.TruthKnown && runState.SteeringWheelRaised;
            case WorldPropId.Rope:
                return runState.RopeTaken;
            default:
                return true;
        }
    }
}
