using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class CameraZone : MonoBehaviour
{
    [SerializeField]
    private CameraZoneController controller;

    [SerializeField]
    private CameraZoneSettings settings = CameraZoneSettings.Default;

    [SerializeField]
    private int priority;

    public CameraZoneSettings Settings => settings;
    public int Priority => priority;

    private int playerTouchCount;

    private void Reset()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    private void OnValidate()
    {
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerTouchCount++;
        if (playerTouchCount > 1)
        {
            return;
        }

        if (controller == null)
        {
            controller = FindObjectOfType<CameraZoneController>();
        }

        controller?.EnterZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerTouchCount = Mathf.Max(0, playerTouchCount - 1);
        if (playerTouchCount > 0)
        {
            return;
        }
        controller?.ExitZone(this);
    }

    private static bool IsPlayer(Collider other)
    {
        return other != null && other.GetComponentInParent<Player>() != null;
    }
}
