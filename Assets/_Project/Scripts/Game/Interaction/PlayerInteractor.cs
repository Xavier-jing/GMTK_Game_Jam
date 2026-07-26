using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInteractionDetector))]
public sealed class PlayerInteractor : MonoBehaviour
{
    private readonly List<IInteractable> nearbyInteractables =
        new List<IInteractable>(8);

    private PlayerInteractionDetector interactionDetector;
    private PlayerCarrySlot carrySlot;
    private GamePause gamePause;
    private InteractionContext interactionContext;
    private IInteractable currentTarget;
    private string currentPrompt = string.Empty;
    private bool currentTargetCanInteract;
    private bool isInteracting;
    private bool interactPressed;

    public event Action<string, bool> PromptChanged;

    public IInteractable CurrentTarget => currentTarget;

    public string CurrentPrompt => currentPrompt;

    public bool CurrentTargetCanInteract => currentTargetCanInteract;

    private void Awake()
    {
        interactionDetector = GetComponent<PlayerInteractionDetector>();
        carrySlot = GetComponent<PlayerCarrySlot>();
    }

    private void Start()
    {
        AppContext appContext = AppContext.Instance;
        gamePause = appContext.GamePause;
        interactionContext = new InteractionContext(gameObject, appContext.Inventory);
    }

    private void OnEnable()
    {
        PlayerInputHandler.OnInteractPressed += HandleInteractPressed;
    }

    private void OnDisable()
    {
        PlayerInputHandler.OnInteractPressed -= HandleInteractPressed;
        nearbyInteractables.Clear();
        SetCurrentTarget(null, false);
        isInteracting = false;
        interactPressed = false;
    }

    private void Update()
    {
        bool approachedStoryTarget = RefreshCurrentTarget();

        if (gamePause == null || gamePause.IsPaused || isInteracting)
        {
            interactPressed = false;
            return;
        }

        if (approachedStoryTarget)
        {
            InteractWithCurrentTarget();
            return;
        }

        if (!interactPressed)
        {
            return;
        }

        interactPressed = false;
        InteractWithCurrentTarget();
    }

    private void HandleInteractPressed(PlayerInputHandler inputHandler)
    {
        if (inputHandler != null && inputHandler.gameObject == gameObject)
        {
            interactPressed = true;
        }
    }

    private bool RefreshCurrentTarget()
    {
        interactionDetector.GetNearbyInteractables(nearbyInteractables);

        WorldStoryInteractable carriedProp =
            carrySlot != null ? carrySlot.CurrentProp : null;

        IInteractable nearestAvailable = null;
        IInteractable nearestBlocked = null;
        float nearestAvailableDistance = float.PositiveInfinity;
        float nearestBlockedDistance = float.PositiveInfinity;
        int nearestAvailableId = int.MaxValue;
        int nearestBlockedId = int.MaxValue;

        for (int i = 0; i < nearbyInteractables.Count; i++)
        {
            IInteractable interactable = nearbyInteractables[i];
            Component component = interactable as Component;
            if (component == null ||
                component.gameObject == null ||
                !component.gameObject.activeInHierarchy)
            {
                continue;
            }

            Transform point = interactable.InteractionPoint;
            if (point == null)
            {
                continue;
            }

            float sqrDistance =
                (point.position - transform.position).sqrMagnitude;
            int instanceId = component.GetInstanceID();
            bool canInteract = interactable.CanInteract(interactionContext);

            if (canInteract &&
                IsPreferred(
                    sqrDistance,
                    instanceId,
                    nearestAvailableDistance,
                    nearestAvailableId))
            {
                nearestAvailable = interactable;
                nearestAvailableDistance = sqrDistance;
                nearestAvailableId = instanceId;
            }
            else if (!canInteract &&
                     IsPreferred(
                         sqrDistance,
                         instanceId,
                         nearestBlockedDistance,
                         nearestBlockedId))
            {
                nearestBlocked = interactable;
                nearestBlockedDistance = sqrDistance;
                nearestBlockedId = instanceId;
            }
        }

        IInteractable selectedTarget = nearestAvailable ?? nearestBlocked;
        bool selectedFromProximity = selectedTarget != null;
        bool selectedCanInteract = nearestAvailable != null;

        if (selectedTarget == null &&
            carriedProp != null &&
            carriedProp.isActiveAndEnabled &&
            carriedProp.CanInteract(interactionContext))
        {
            selectedTarget = carriedProp;
            selectedCanInteract = true;
        }

        bool targetChanged = SetCurrentTarget(
            selectedTarget,
            selectedCanInteract);

        // Availability changes on the same target do not count as a new
        // approach. This prevents a completed Choice story from reopening
        // immediately while the player is still inside the detection radius.
        return targetChanged &&
               selectedFromProximity &&
               currentTargetCanInteract &&
               selectedTarget is WorldStoryInteractable;
    }

    private void InteractWithCurrentTarget()
    {
        IInteractable target = currentTarget;
        Component component = target as Component;

        // This is intentionally checked again immediately before dispatch.
        // Conditions shown in UI may have changed after target selection.
        if (target == null ||
            component == null ||
            !component.gameObject.activeInHierarchy ||
            !target.CanInteract(interactionContext))
        {
            RefreshCurrentTarget();
            return;
        }

        isInteracting = true;
        try
        {
            target.Interact(interactionContext);
        }
        finally
        {
            isInteracting = false;
        }

        RefreshCurrentTarget();
    }

    private bool SetCurrentTarget(IInteractable target, bool canInteract)
    {
        string prompt = target != null
            ? target.GetInteractionPrompt(interactionContext) ?? string.Empty
            : string.Empty;
        bool changed =
            currentTarget != target ||
            currentTargetCanInteract != canInteract ||
            !string.Equals(currentPrompt, prompt, StringComparison.Ordinal);

        if (!changed)
        {
            return false;
        }

        bool targetChanged = currentTarget != target;
        currentTarget = target;
        currentTargetCanInteract = canInteract;
        currentPrompt = prompt;
        PromptChanged?.Invoke(currentPrompt, currentTargetCanInteract);
        return targetChanged;
    }

    private static bool IsPreferred(
        float sqrDistance,
        int instanceId,
        float currentSqrDistance,
        int currentInstanceId)
    {
        return sqrDistance < currentSqrDistance ||
               (Mathf.Approximately(sqrDistance, currentSqrDistance) &&
                instanceId < currentInstanceId);
    }
}
