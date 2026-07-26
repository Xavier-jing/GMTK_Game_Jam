using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerInteractionDetector : MonoBehaviour
{
    private readonly Collider[] overlapResults = new Collider[16];
    private readonly List<IInteractable> interactables = new List<IInteractable>(8);
    private readonly HashSet<IInteractable> uniqueInteractables = new HashSet<IInteractable>();

    [SerializeField]
    private float detectionRadius = 1.5f;

    [SerializeField]
    [Min(0f)]
    private float detectionHeight = 4f;

    [SerializeField]
    private Vector3 detectionOffset;

    [SerializeField]
    private LayerMask interactableLayers = ~0;

    public float DetectionRadius => detectionRadius;

    public int GetNearbyInteractables(List<IInteractable> results)
    {
        results.Clear();
        interactables.Clear();
        uniqueInteractables.Clear();

        Vector3 bottom = transform.position + detectionOffset;
        Vector3 top = bottom + Vector3.up * detectionHeight;
        int hitCount = Physics.OverlapCapsuleNonAlloc(
            bottom,
            top,
            detectionRadius,
            overlapResults,
            interactableLayers,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapResults[i];
            if (hit == null)
            {
                continue;
            }

            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null || uniqueInteractables.Contains(interactable))
            {
                continue;
            }

            uniqueInteractables.Add(interactable);
            interactables.Add(interactable);
        }

        results.AddRange(interactables);
        return results.Count;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 bottom = transform.position + detectionOffset;
        Vector3 top = bottom + Vector3.up * detectionHeight;
        Gizmos.DrawWireSphere(bottom, detectionRadius);
        Gizmos.DrawWireSphere(top, detectionRadius);
        Gizmos.DrawLine(
            bottom + Vector3.forward * detectionRadius,
            top + Vector3.forward * detectionRadius);
        Gizmos.DrawLine(
            bottom - Vector3.forward * detectionRadius,
            top - Vector3.forward * detectionRadius);
        Gizmos.DrawLine(
            bottom + Vector3.right * detectionRadius,
            top + Vector3.right * detectionRadius);
        Gizmos.DrawLine(
            bottom - Vector3.right * detectionRadius,
            top - Vector3.right * detectionRadius);
    }
}
