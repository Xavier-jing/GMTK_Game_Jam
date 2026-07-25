using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerInteractor : MonoBehaviour
{
    private sealed class Candidate
    {
        public readonly MonoBehaviour Behaviour;
        public readonly IInteractable Interactable;

        public int OverlapCount;
        public bool MissingInteractionPointReported;

        public Candidate(MonoBehaviour behaviour, IInteractable interactable)
        {
            Behaviour = behaviour;
            Interactable = interactable;
            OverlapCount = 1;
        }
    }

    [SerializeField]
    private CircleCollider2D interactionSensor;

    private readonly Dictionary<MonoBehaviour, Candidate> candidates =
        new Dictionary<MonoBehaviour, Candidate>();

    private readonly Dictionary<Collider2D, MonoBehaviour> colliderOwners =
        new Dictionary<Collider2D, MonoBehaviour>();

    private readonly List<MonoBehaviour> staleCandidates = new List<MonoBehaviour>();

    private readonly List<KeyValuePair<Collider2D, MonoBehaviour>> staleColliderMappings =
        new List<KeyValuePair<Collider2D, MonoBehaviour>>();

    private readonly List<Collider2D> overlappingColliders = new List<Collider2D>();

    private Candidate currentCandidate;
    private GamePause gamePause;
    private InputReader input;
    private InteractionContext interactionContext;
    private string currentPrompt = string.Empty;
    private bool currentTargetCanInteract;
    private bool isInteracting;

    public event Action<string, bool> PromptChanged;

    public IInteractable CurrentTarget =>
        currentCandidate != null ? currentCandidate.Interactable : null;

    public string CurrentPrompt => currentPrompt;

    public bool CurrentTargetCanInteract => currentTargetCanInteract;

    private void Awake()
    {
        if (interactionSensor == null)
        {
            Debug.LogError(
                $"PlayerInteractor on '{name}' is missing its interaction sensor reference.",
                this);
            enabled = false;
            return;
        }

        if (interactionSensor.gameObject != gameObject)
        {
            Debug.LogError(
                $"PlayerInteractor on '{name}' requires its interaction sensor on the same GameObject.",
                this);
            enabled = false;
            return;
        }

        if (!interactionSensor.isTrigger)
        {
            Debug.LogError(
                $"PlayerInteractor on '{name}' requires interaction sensor '{interactionSensor.name}' " +
                "to have Is Trigger enabled.",
                this);
            enabled = false;
        }
    }

    private void Start()
    {
        AppContext appContext = AppContext.Instance;
        gamePause = appContext.GamePause;
        input = appContext.Input;
        interactionContext = new InteractionContext(gameObject, appContext.Inventory);
    }

    private void OnEnable()
    {
        if (interactionSensor == null ||
            !interactionSensor.enabled ||
            !interactionSensor.gameObject.activeInHierarchy)
        {
            return;
        }

        overlappingColliders.Clear();
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.NoFilter();
        interactionSensor.OverlapCollider(contactFilter, overlappingColliders);

        foreach (Collider2D overlappingCollider in overlappingColliders)
        {
            RegisterCandidate(overlappingCollider);
        }
    }

    private void OnDisable()
    {
        candidates.Clear();
        colliderOwners.Clear();
        staleCandidates.Clear();
        staleColliderMappings.Clear();
        overlappingColliders.Clear();
        SetCurrentCandidate(null, false);
        isInteracting = false;
    }

    private void Update()
    {
        RefreshCurrentTarget();

        if (gamePause == null || input == null || gamePause.IsPaused || isInteracting)
        {
            return;
        }

        if (input.SubmitPressedThisFrame)
        {
            InteractWithCurrentTarget();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        RegisterCandidate(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (!colliderOwners.TryGetValue(other, out MonoBehaviour behaviour))
        {
            return;
        }

        colliderOwners.Remove(other);

        if (!candidates.TryGetValue(behaviour, out Candidate candidate))
        {
            return;
        }

        candidate.OverlapCount--;
        if (candidate.OverlapCount > 0)
        {
            return;
        }

        candidates.Remove(behaviour);
        if (currentCandidate == candidate)
        {
            RefreshCurrentTarget();
        }
    }

    private void RefreshCurrentTarget()
    {
        RemoveDestroyedCandidates();

        Candidate nearestAvailableCandidate = null;
        Candidate nearestBlockedCandidate = null;
        float nearestAvailableSqrDistance = float.PositiveInfinity;
        float nearestBlockedSqrDistance = float.PositiveInfinity;
        int nearestAvailableInstanceId = int.MaxValue;
        int nearestBlockedInstanceId = int.MaxValue;
        Vector2 interactorPosition = transform.position;

        foreach (Candidate candidate in candidates.Values)
        {
            MonoBehaviour behaviour = candidate.Behaviour;
            if (!behaviour.isActiveAndEnabled)
            {
                continue;
            }

            Transform interactionPoint = candidate.Interactable.InteractionPoint;
            if (interactionPoint == null)
            {
                ReportMissingInteractionPoint(candidate);
                continue;
            }

            float sqrDistance = ((Vector2)interactionPoint.position - interactorPosition).sqrMagnitude;
            int instanceId = behaviour.GetInstanceID();
            bool canInteract = candidate.Interactable.CanInteract(interactionContext);

            if (canInteract)
            {
                if (!IsPreferredCandidate(
                        sqrDistance,
                        instanceId,
                        nearestAvailableSqrDistance,
                        nearestAvailableInstanceId))
                {
                    continue;
                }

                nearestAvailableCandidate = candidate;
                nearestAvailableSqrDistance = sqrDistance;
                nearestAvailableInstanceId = instanceId;
                continue;
            }

            if (!IsPreferredCandidate(
                    sqrDistance,
                    instanceId,
                    nearestBlockedSqrDistance,
                    nearestBlockedInstanceId))
            {
                continue;
            }

            nearestBlockedCandidate = candidate;
            nearestBlockedSqrDistance = sqrDistance;
            nearestBlockedInstanceId = instanceId;
        }

        if (nearestAvailableCandidate != null)
        {
            SetCurrentCandidate(nearestAvailableCandidate, true);
            return;
        }

        SetCurrentCandidate(nearestBlockedCandidate, false);
    }

    private void RemoveDestroyedCandidates()
    {
        staleCandidates.Clear();
        staleColliderMappings.Clear();

        foreach (KeyValuePair<Collider2D, MonoBehaviour> pair in colliderOwners)
        {
            if (pair.Key == null ||
                pair.Value == null ||
                !pair.Key.enabled ||
                !pair.Key.gameObject.activeInHierarchy)
            {
                staleColliderMappings.Add(pair);
            }
        }

        foreach (KeyValuePair<Collider2D, MonoBehaviour> pair in staleColliderMappings)
        {
            colliderOwners.Remove(pair.Key);

            if (pair.Value != null &&
                candidates.TryGetValue(pair.Value, out Candidate candidate))
            {
                candidate.OverlapCount--;
                if (candidate.OverlapCount <= 0)
                {
                    candidates.Remove(pair.Value);
                }
            }
        }

        foreach (KeyValuePair<MonoBehaviour, Candidate> pair in candidates)
        {
            if (pair.Key == null)
            {
                staleCandidates.Add(pair.Key);
            }
        }

        foreach (MonoBehaviour staleCandidate in staleCandidates)
        {
            candidates.Remove(staleCandidate);
        }
    }

    private void SetCurrentCandidate(Candidate candidate, bool canInteract)
    {
        string prompt = candidate != null
            ? candidate.Interactable.GetInteractionPrompt(interactionContext) ?? string.Empty
            : string.Empty;

        bool targetChanged = currentCandidate != candidate;
        bool promptChanged = !string.Equals(currentPrompt, prompt, StringComparison.Ordinal);
        bool availabilityChanged = currentTargetCanInteract != canInteract;
        if (!targetChanged && !promptChanged && !availabilityChanged)
        {
            return;
        }

        currentCandidate = candidate;
        currentPrompt = prompt;
        currentTargetCanInteract = canInteract;
        PromptChanged?.Invoke(currentPrompt, currentTargetCanInteract);
    }

    private void InteractWithCurrentTarget()
    {
        Candidate candidate = currentCandidate;
        if (candidate == null ||
            candidate.Behaviour == null ||
            !candidate.Behaviour.isActiveAndEnabled ||
            !candidate.Interactable.CanInteract(interactionContext))
        {
            RefreshCurrentTarget();
            return;
        }

        isInteracting = true;
        try
        {
            candidate.Interactable.Interact(interactionContext);
        }
        finally
        {
            isInteracting = false;
        }

        RefreshCurrentTarget();
    }

    private static bool IsPreferredCandidate(
        float sqrDistance,
        int instanceId,
        float currentSqrDistance,
        int currentInstanceId)
    {
        return sqrDistance < currentSqrDistance ||
               (Mathf.Approximately(sqrDistance, currentSqrDistance) &&
                instanceId < currentInstanceId);
    }

    private void ReportMissingInteractionPoint(Candidate candidate)
    {
        if (candidate.MissingInteractionPointReported)
        {
            return;
        }

        candidate.MissingInteractionPointReported = true;
        Debug.LogWarning(
            $"Interactable '{candidate.Behaviour.name}' returned a null InteractionPoint and was ignored.",
            candidate.Behaviour);
    }

    private void RegisterCandidate(Collider2D source)
    {
        if (source == null || colliderOwners.ContainsKey(source))
        {
            return;
        }

        if (!TryResolveInteractable(source, out MonoBehaviour behaviour, out IInteractable interactable))
        {
            return;
        }

        colliderOwners.Add(source, behaviour);

        if (candidates.TryGetValue(behaviour, out Candidate candidate))
        {
            candidate.OverlapCount++;
            return;
        }

        candidates.Add(behaviour, new Candidate(behaviour, interactable));
    }

    private static bool TryResolveInteractable(
        Collider2D source,
        out MonoBehaviour behaviour,
        out IInteractable interactable)
    {
        MonoBehaviour[] behaviours = source.GetComponentsInParent<MonoBehaviour>(true);
        foreach (MonoBehaviour candidate in behaviours)
        {
            if (candidate is IInteractable resolvedInteractable)
            {
                behaviour = candidate;
                interactable = resolvedInteractable;
                return true;
            }
        }

        behaviour = null;
        interactable = null;
        return false;
    }
}
