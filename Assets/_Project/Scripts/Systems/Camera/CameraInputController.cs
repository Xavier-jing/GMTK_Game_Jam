using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class CameraInputController : MonoBehaviour
{
    private enum PanMode
    {
        FollowingPlayer,
        Dragging
    }

    [Header("References")]
    [SerializeField]
    private CameraZoneController zoneController;

    [SerializeField]
    private Camera renderCamera;

    [SerializeField]
    private Transform playerTarget;

    [SerializeField]
    private Rigidbody panRigidbody;

    [Header("Zoom")]
    [SerializeField]
    private bool enableScrollZoom = true;

    [SerializeField]
    private float zoomStep = 1.2f;

    [SerializeField]
    private float minOrthographicSize = 4.5f;

    [SerializeField]
    private float maxOrthographicSize = 12f;

    [Header("Drag Pan")]
    [SerializeField]
    private bool enableDragPan = true;

    [SerializeField]
    private int dragMouseButton;

    [SerializeField]
    private float dragSensitivity = 1f;

    private CinemachineVirtualCamera virtualCamera;
    private PanMode panMode = PanMode.FollowingPlayer;
    private Collider[] panColliders;
    private Vector3 previousDragWorldPosition;
    private float zoomOffset;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();

        if (playerTarget != null)
        {
            CachePanColliders();
            SetPanCollisionEnabled(false);
            FollowPlayer();
        }
    }

    private void Update()
    {
        HandleZoom();
        HandleDragInput();
        zoneController.SetManualOrthographicSizeOffset(zoomOffset);
    }

    private void HandleZoom()
    {
        if (!enableScrollZoom || zoneController == null || IsPointerOverUi())
        {
            return;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scroll, 0f))
        {
            return;
        }

        float baseSize = zoneController.CurrentBaseOrthographicSize;
        float desiredSize = Mathf.Clamp(
            baseSize + zoomOffset - scroll * zoomStep,
            minOrthographicSize,
            maxOrthographicSize);
        zoomOffset = desiredSize - baseSize;
    }

    private void HandleDragInput()
    {
        if (!enableDragPan || renderCamera == null || playerTarget == null || panRigidbody == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(dragMouseButton))
        {
            BeginDrag();
        }

        if (panMode == PanMode.Dragging && Input.GetMouseButton(dragMouseButton))
        {
            DragPanTarget();
        }

        if (panMode == PanMode.Dragging && Input.GetMouseButtonUp(dragMouseButton))
        {
            FollowPlayer();
        }
    }

    private void BeginDrag()
    {
        panMode = PanMode.Dragging;
        panRigidbody.position = playerTarget.position;
        panRigidbody.rotation = playerTarget.rotation;
        panRigidbody.velocity = Vector3.zero;
        panRigidbody.angularVelocity = Vector3.zero;
        panRigidbody.isKinematic = false;
        IgnorePlayerCollisions();
        SetPanCollisionEnabled(true);
        virtualCamera.Follow = panRigidbody.transform;
        previousDragWorldPosition = GetMouseWorldPosition(Input.mousePosition);
    }

    private void DragPanTarget()
    {
        Vector3 currentDragWorldPosition = GetMouseWorldPosition(Input.mousePosition);
        Vector3 worldDelta = previousDragWorldPosition - currentDragWorldPosition;
        previousDragWorldPosition = currentDragWorldPosition;

        Vector3 targetPosition = panRigidbody.position + worldDelta * dragSensitivity;
        targetPosition.y = panRigidbody.position.y;
        panRigidbody.MovePosition(targetPosition);
    }

    private void FollowPlayer()
    {
        panMode = PanMode.FollowingPlayer;
        SetPanCollisionEnabled(false);

        if (virtualCamera != null && playerTarget != null)
        {
            virtualCamera.Follow = playerTarget;
            virtualCamera.PreviousStateIsValid = false;
        }
    }

    private void CachePanColliders()
    {
        if (panRigidbody == null)
        {
            panColliders = new Collider[0];
            return;
        }

        panColliders = panRigidbody.GetComponentsInChildren<Collider>(true);
    }

    private void SetPanCollisionEnabled(bool enabled)
    {
        if (panColliders == null)
        {
            return;
        }

        for (int i = 0; i < panColliders.Length; i++)
        {
            if (panColliders[i] != null)
            {
                panColliders[i].enabled = enabled;
            }
        }
    }

    private void IgnorePlayerCollisions()
    {
        Collider[] playerColliders = playerTarget.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < panColliders.Length; i++)
        {
            Collider panCollider = panColliders[i];
            if (panCollider == null)
            {
                continue;
            }

            for (int j = 0; j < playerColliders.Length; j++)
            {
                Collider playerCollider = playerColliders[j];
                if (playerCollider != null)
                {
                    Physics.IgnoreCollision(panCollider, playerCollider, true);
                }
            }
        }
    }

    private Vector3 GetMouseWorldPosition(Vector3 mousePosition)
    {
        Ray ray = renderCamera.ScreenPointToRay(mousePosition);
        float planeHeight = panRigidbody != null
            ? panRigidbody.position.y
            : playerTarget.position.y;
        Plane panPlane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));

        return panPlane.Raycast(ray, out float distance)
            ? ray.GetPoint(distance)
            : Vector3.zero;
    }

    private void OnDestroy()
    {
        FollowPlayer();
    }


    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
