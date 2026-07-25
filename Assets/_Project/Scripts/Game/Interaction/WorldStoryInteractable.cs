using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldStoryInteractable : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [SerializeField]
    private WorldPropId propId;

    [SerializeField]
    private Transform interactionPoint;

    [Header("Story")]
    [SerializeField]
    private StoryController storyController;

    [SerializeField]
    private string firstScriptId;

    [SerializeField]
    private string repeatScriptId;

    [SerializeField]
    private bool autoStartFirstStory;

    [SerializeField]
    private string interactionPrompt = "F";

    [Header("Inventory items")]
    [SerializeField]
    private ItemDefinition inventoryItem;

    [SerializeField]
    private ItemDefinition requiredItem;

    [Header("World presentation")]
    [SerializeField]
    private GameObject presentationRoot;

    [SerializeField]
    private Renderer[] presentationRenderers = Array.Empty<Renderer>();

    [SerializeField]
    private Collider[] interactionColliders = Array.Empty<Collider>();

    private LoopProgress loopProgress;
    private RunState runState;
    private bool completedFirstStory;
    private bool isCarried;
    private bool interactionDisabled;
    private bool removedFromWorld;
    private bool subscribedToStory;
    private string pendingScriptId;
    private bool pendingWasFirstStory;

    public WorldPropId PropId => propId;

    public bool IsCarried => isCarried;

    public Transform InteractionPoint =>
        interactionPoint != null ? interactionPoint : transform;

    private void Awake()
    {
        if (propId == WorldPropId.None ||
            !Enum.IsDefined(typeof(WorldPropId), propId))
        {
            Debug.LogError(
                $"WorldStoryInteractable on '{name}' requires a Prop Id.",
                this);
            enabled = false;
            return;
        }

        if (presentationRenderers == null || presentationRenderers.Length == 0)
        {
            presentationRenderers = GetComponentsInChildren<Renderer>(true);
        }

        if (interactionColliders == null || interactionColliders.Length == 0)
        {
            interactionColliders = GetComponentsInChildren<Collider>(true);
        }
    }

    private void OnEnable()
    {
        ResolveRuntimeReferences();
        SubscribeToStory();

        if (loopProgress != null)
        {
            loopProgress.Changed += RefreshPresentation;
        }

        if (runState != null)
        {
            runState.Changed += RefreshPresentation;
        }

        RefreshPresentation();
    }

    private void Start()
    {
        if (autoStartFirstStory && !completedFirstStory && IsAvailableInWorld())
        {
            TryStartConfiguredStory();
        }
    }

    private void OnDisable()
    {
        if (loopProgress != null)
        {
            loopProgress.Changed -= RefreshPresentation;
        }

        if (runState != null)
        {
            runState.Changed -= RefreshPresentation;
        }

        UnsubscribeFromStory();
    }

    public string GetInteractionPrompt(InteractionContext context)
    {
        return interactionPrompt ?? string.Empty;
    }

    public bool CanInteract(InteractionContext context)
    {
        ResolveRuntimeReferences();
        return !interactionDisabled &&
               !removedFromWorld &&
               (isCarried || IsAvailableInWorld()) &&
               storyController != null &&
               !storyController.IsRunning &&
               !string.IsNullOrWhiteSpace(GetConfiguredScriptId());
    }

    public void Interact(InteractionContext context)
    {
        if (!CanInteract(context))
        {
            return;
        }

        TryStartConfiguredStory();
    }

    public bool CanExecuteCommand(
        StoryActionContext context,
        WorldPropCommand command,
        out string reason)
    {
        if (context == null)
        {
            reason = "The story action context is missing.";
            return false;
        }

        if (removedFromWorld)
        {
            reason = $"World prop '{propId}' has already left the current run.";
            return false;
        }

        if (!isCarried && !IsAvailableInWorld())
        {
            reason = $"World prop '{propId}' is not available in the current progress state.";
            return false;
        }

        switch (command)
        {
            case WorldPropCommand.Inspect:
                return Succeed(out reason);

            case WorldPropCommand.OpenDresser:
                return Require(
                    propId == WorldPropId.Dresser && !context.RunState.DresserOpened,
                    "The dresser is already open or this target is not the dresser.",
                    out reason);

            case WorldPropCommand.TakeIntoCarrySlot:
                return Require(
                    context.CarrySlot != null && context.CarrySlot.CanTake(this),
                    "The prop cannot enter the carry slot now.",
                    out reason);

            case WorldPropCommand.DropFromCarrySlot:
                return Require(
                    context.CarrySlot != null && context.CarrySlot.CanDrop(this),
                    "This prop is not the item currently in the carry slot.",
                    out reason);

            case WorldPropCommand.AcquireInventoryItem:
                return Require(
                    WorldPropRules.IsInventoryItem(propId) &&
                    context.Inventory != null &&
                    context.Inventory.GetAddResult(inventoryItem) == InventoryAddResult.Added,
                    "The item is missing its definition, is already owned, or the inventory is full.",
                    out reason);

            case WorldPropCommand.FirstWallStrike:
                return Require(
                    propId == WorldPropId.SmallWallHole &&
                    HasWallToolPrerequisites(context) &&
                    !context.LoopProgress.WallStruckOnce,
                    "The first wall strike prerequisites are no longer satisfied.",
                    out reason);

            case WorldPropCommand.SecondWallStrike:
                return Require(
                    propId == WorldPropId.LargeWallHole &&
                    HasWallToolPrerequisites(context) &&
                    context.LoopProgress.WallStruckOnce &&
                    !context.LoopProgress.TruthKnown,
                    "The second wall strike prerequisites are no longer satisfied.",
                    out reason);

            case WorldPropCommand.CutBlanket:
                return Require(
                    propId == WorldPropId.BedBlanket &&
                    context.LoopProgress.TruthKnown &&
                    !context.RunState.FabricPrepared &&
                    requiredItem != null &&
                    context.Inventory.Contains(requiredItem),
                    "Cutting the blanket requires the truth event and the configured scissors item.",
                    out reason);

            case WorldPropCommand.TriggerBedSwitch:
                return Require(
                    propId == WorldPropId.BedSwitch &&
                    context.LoopProgress.TruthKnown &&
                    context.RunState.FabricPrepared &&
                    !context.RunState.BedSwitchTriggered,
                    "The bed switch prerequisites are no longer satisfied.",
                    out reason);

            case WorldPropCommand.StartSteeringWheel:
                return Require(
                    propId == WorldPropId.SteeringWheel &&
                    context.LoopProgress.TruthKnown &&
                    context.RunState.BedSwitchTriggered &&
                    !context.RunState.SteeringWheelRaised,
                    "The steering wheel prerequisites are no longer satisfied.",
                    out reason);

            case WorldPropCommand.UnplugRefrigerator:
                return Require(
                    propId == WorldPropId.Refrigerator &&
                    context.LoopProgress.TruthKnown &&
                    !context.RunState.FridgeUnplugged,
                    "The refrigerator cannot be unplugged now.",
                    out reason);

            case WorldPropCommand.ConnectBedPower:
                return Require(
                    propId == WorldPropId.PowerConnector &&
                    context.LoopProgress.TruthKnown &&
                    context.RunState.FridgeUnplugged &&
                    context.RunState.SteeringWheelRaised &&
                    !context.RunState.BedConnected &&
                    IsCarrying(context, WorldPropId.CableBed),
                    "Connecting power requires the active steering wheel, the unplugged refrigerator, and the cable bed in the carry slot.",
                    out reason);

            case WorldPropCommand.InstallPlank:
                return Require(
                    propId == WorldPropId.SmallWallHole &&
                    context.LoopProgress.TruthKnown &&
                    context.LoopProgress.WallStruckOnce &&
                    !context.RunState.WallRepaired &&
                    IsCarrying(context, WorldPropId.Plank),
                    "Installing the plank requires the revealed wall and the plank in the carry slot.",
                    out reason);

            default:
                reason = $"World prop command '{command}' is not supported.";
                return false;
        }
    }

    public bool TryExecuteCommand(
        StoryActionContext context,
        WorldPropCommand command,
        out string reason)
    {
        if (!CanExecuteCommand(context, command, out reason))
        {
            return false;
        }

        switch (command)
        {
            case WorldPropCommand.Inspect:
                return true;

            case WorldPropCommand.OpenDresser:
                context.RunState.MarkDresserOpened();
                return true;

            case WorldPropCommand.TakeIntoCarrySlot:
                return context.CarrySlot.TryTake(this);

            case WorldPropCommand.DropFromCarrySlot:
                return context.CarrySlot.TryDrop(this);

            case WorldPropCommand.AcquireInventoryItem:
                if (context.Inventory.TryAdd(inventoryItem) != InventoryAddResult.Added)
                {
                    reason = "The inventory changed before the item could be added.";
                    return false;
                }

                if (propId == WorldPropId.Wrench && context.Player != null)
                {
                    context.Player.GameplayStatus.AcquireWrench();
                }

                removedFromWorld = true;
                RefreshPresentation();
                return true;

            case WorldPropCommand.FirstWallStrike:
                return context.TryRequestRunEnd(
                    RunEndReason.TurnsExhausted,
                    context.LoopProgress.MarkWallStruckOnce,
                    out reason);

            case WorldPropCommand.SecondWallStrike:
                return context.TryRequestRunEnd(
                    RunEndReason.TruthRevealed,
                    null,
                    out reason);

            case WorldPropCommand.CutBlanket:
                context.RunState.MarkFabricPrepared();
                return true;

            case WorldPropCommand.TriggerBedSwitch:
                context.RunState.MarkBedSwitchTriggered();
                return true;

            case WorldPropCommand.StartSteeringWheel:
                context.RunState.MarkSteeringWheelRaised();
                return true;

            case WorldPropCommand.UnplugRefrigerator:
                context.RunState.MarkFridgeUnplugged();
                return true;

            case WorldPropCommand.ConnectBedPower:
                if (!TryPlaceCarriedProp(context, WorldPropId.CableBed))
                {
                    reason = "The cable bed left the carry slot before it could be connected.";
                    return false;
                }

                context.RunState.MarkBedConnected();
                return true;

            case WorldPropCommand.InstallPlank:
                if (!TryPlaceCarriedProp(context, WorldPropId.Plank))
                {
                    reason = "The plank left the carry slot before it could be installed.";
                    return false;
                }

                context.RunState.MarkWallRepaired();
                return true;

            default:
                reason = $"World prop command '{command}' is not supported.";
                return false;
        }
    }

    public void SetCarried(bool value)
    {
        isCarried = value;
        RefreshPresentation();
    }

    public void ReleaseFromCarrySlot(Vector3 position, bool remainsInteractable)
    {
        transform.position = position;
        isCarried = false;
        interactionDisabled = !remainsInteractable;
        RefreshPresentation();
    }

    private void ResolveRuntimeReferences()
    {
        if (loopProgress == null || runState == null)
        {
            AppContext appContext = AppContext.Instance;
            loopProgress = appContext.LoopProgress;
            runState = appContext.RunState;
        }

        if (storyController == null)
        {
            storyController = FindObjectOfType<StoryController>();
        }

        SubscribeToStory();
    }

    private void SubscribeToStory()
    {
        if (storyController == null || subscribedToStory)
        {
            return;
        }

        storyController.Completed += HandleStoryCompleted;
        storyController.Failed += HandleStoryFailed;
        subscribedToStory = true;
    }

    private void UnsubscribeFromStory()
    {
        if (storyController == null || !subscribedToStory)
        {
            return;
        }

        storyController.Completed -= HandleStoryCompleted;
        storyController.Failed -= HandleStoryFailed;
        subscribedToStory = false;
    }

    private void HandleStoryCompleted(StoryCompletion completion)
    {
        if (completion == null ||
            !string.Equals(
                completion.RootScriptId,
                pendingScriptId,
                StringComparison.Ordinal))
        {
            return;
        }

        if (pendingWasFirstStory)
        {
            completedFirstStory = true;
        }

        pendingScriptId = string.Empty;
        pendingWasFirstStory = false;
    }

    private void HandleStoryFailed(StoryError error)
    {
        pendingScriptId = string.Empty;
        pendingWasFirstStory = false;
    }

    private bool TryStartConfiguredStory()
    {
        ResolveRuntimeReferences();
        if (storyController == null || storyController.IsRunning)
        {
            return false;
        }

        string scriptId = GetConfiguredScriptId();
        if (string.IsNullOrWhiteSpace(scriptId))
        {
            return false;
        }

        bool wasFirstStory = !completedFirstStory;
        if (!storyController.TryStart(scriptId))
        {
            return false;
        }

        pendingScriptId = scriptId;
        pendingWasFirstStory = wasFirstStory;
        return true;
    }

    private string GetConfiguredScriptId()
    {
        if (!completedFirstStory || string.IsNullOrWhiteSpace(repeatScriptId))
        {
            return firstScriptId;
        }

        return repeatScriptId;
    }

    private bool IsAvailableInWorld()
    {
        return WorldPropRules.IsPresent(propId, loopProgress, runState);
    }

    private void RefreshPresentation()
    {
        bool shouldShow = !removedFromWorld &&
                          !isCarried &&
                          IsAvailableInWorld();

        if (presentationRoot != null && presentationRoot != gameObject)
        {
            presentationRoot.SetActive(shouldShow);
        }

        for (int i = 0; i < presentationRenderers.Length; i++)
        {
            Renderer renderer = presentationRenderers[i];
            if (renderer != null)
            {
                renderer.enabled = shouldShow;
            }
        }

        bool shouldEnableInteraction = shouldShow && !interactionDisabled;
        for (int i = 0; i < interactionColliders.Length; i++)
        {
            Collider interactionCollider = interactionColliders[i];
            if (interactionCollider != null)
            {
                interactionCollider.enabled = shouldEnableInteraction;
            }
        }
    }

    private bool TryPlaceCarriedProp(
        StoryActionContext context,
        WorldPropId expectedPropId)
    {
        WorldStoryInteractable carriedProp = context.CarrySlot.CurrentProp;
        return carriedProp != null &&
               carriedProp.PropId == expectedPropId &&
               context.CarrySlot.TryDropAt(
                   carriedProp,
                   InteractionPoint.position,
                   false);
    }

    private static bool IsCarrying(
        StoryActionContext context,
        WorldPropId expectedPropId)
    {
        return context.CarrySlot != null &&
               context.CarrySlot.CurrentProp != null &&
               context.CarrySlot.CurrentProp.PropId == expectedPropId;
    }

    private static bool HasWallToolPrerequisites(StoryActionContext context)
    {
        return context.Player != null &&
               context.Player.GameplayStatus.HasWrench &&
               context.Player.GameplayStatus.RailRemoved;
    }

    private static bool Require(
        bool condition,
        string failureReason,
        out string reason)
    {
        reason = condition ? string.Empty : failureReason;
        return condition;
    }

    private static bool Succeed(out string reason)
    {
        reason = string.Empty;
        return true;
    }
}
