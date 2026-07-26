using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(CameraZoneController))]
public class CameraInputManager : MonoBehaviour
{
    [SerializeField]
    private CameraZoneController zoneController;

    [SerializeField]
    private CinemachineVirtualCamera virtualCamera;

    [SerializeField]
    [Min(0.01f)]
    private float dragSensitivity = 1f;

    [SerializeField]
    [Range(0.01f, 1f)]
    [Tooltip("Maximum horizontal drag from the starting position, measured as a fraction of the screen width.")]
    private float maxHorizontalDrag = 0.35f;

    [SerializeField]
    [Min(0.01f)]
    private float returnSpeed = 8f;

    [SerializeField]
    [Range(0.0001f, 0.05f)]
    private float viewportTolerance = 0.0025f;

    private Camera outputCamera;
    private Transform followTarget;
    private float dragScreenX;
    private float returnScreenX;
    private float returnVelocity;
    private bool isDragging;
    private bool isReturning;

    private void Awake()
    {
        if (zoneController == null)
        {
            zoneController = GetComponent<CameraZoneController>();
        }

        if (virtualCamera == null)
        {
            virtualCamera = GetComponent<CinemachineVirtualCamera>();
        }

        outputCamera = Camera.main;
        followTarget = virtualCamera != null ? virtualCamera.Follow : null;

        if (zoneController == null || virtualCamera == null || outputCamera == null || followTarget == null)
        {
            Debug.LogError(
                $"CameraInputManager on '{name}' is missing its controller, virtual camera, " +
                "output camera, or Follow target.");
            enabled = false;
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi())
        {
            BeginDrag();
        }

        if (isDragging && mouse.leftButton.isPressed)
        {
            float normalizedDelta = mouse.delta.ReadValue().x / Mathf.Max(1f, Screen.width);
            dragScreenX += normalizedDelta * dragSensitivity;
            dragScreenX = Mathf.Clamp(
                dragScreenX,
                returnScreenX - maxHorizontalDrag,
                returnScreenX + maxHorizontalDrag);
            zoneController.SetManualHorizontalScreenX(dragScreenX);
        }

        if (isDragging && mouse.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
            isReturning = true;
            returnVelocity = 0f;
        }

        if (isReturning)
        {
            UpdateReturn();
        }
    }

    private void OnDisable()
    {
        isDragging = false;
        isReturning = false;

        if (zoneController != null)
        {
            zoneController.EndManualHorizontalFraming();
        }
    }

    private void BeginDrag()
    {
        Vector3 viewportPosition = outputCamera.WorldToViewportPoint(followTarget.position);
        if (viewportPosition.z <= 0f)
        {
            return;
        }

        returnScreenX = viewportPosition.x;
        dragScreenX = returnScreenX;
        returnVelocity = 0f;

        if (!zoneController.BeginManualHorizontalFraming(dragScreenX))
        {
            return;
        }

        isReturning = false;
        isDragging = true;
    }

    private void UpdateReturn()
    {
        dragScreenX = Mathf.SmoothDamp(
            dragScreenX,
            returnScreenX,
            ref returnVelocity,
            1f / returnSpeed,
            Mathf.Infinity,
            Time.unscaledDeltaTime);
        zoneController.SetManualHorizontalScreenX(dragScreenX);

        float playerViewportX = outputCamera.WorldToViewportPoint(followTarget.position).x;
        if (Mathf.Abs(playerViewportX - returnScreenX) > viewportTolerance ||
            Mathf.Abs(returnVelocity) > viewportTolerance)
        {
            return;
        }

        isReturning = false;
        zoneController.EndManualHorizontalFraming();
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
