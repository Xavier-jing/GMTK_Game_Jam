using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class PlayerGroundProjection : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private SpriteRenderer projectionRenderer;

    [Header("Ground Detection")]
    [SerializeField]
    private LayerMask groundLayers;

    [SerializeField]
    [Min(0.1f)]
    private float maxProjectionDistance = 20f;

    [SerializeField]
    [Min(0f)]
    private float minimumAirHeight = 0.15f;

    [SerializeField]
    [Min(0f)]
    private float surfaceOffset = 0.02f;

    [SerializeField]
    [Tooltip("Recommended for pre-drawn 2D ellipse shadows in the fixed isometric camera.")]
    private bool faceCamera = true;

    [SerializeField]
    [Min(0f)]
    [Tooltip("Moves a camera-facing projection toward the camera to prevent the ground mesh from clipping half of the Sprite.")]
    private float cameraFacingDepthOffset = 0.5f;

    [Header("Height Response")]
    [SerializeField]
    [Min(0.01f)]
    private float fullFadeHeight = 6f;

    [SerializeField]
    [Range(0.01f, 2f)]
    private float nearScaleMultiplier = 1f;

    [SerializeField]
    [Range(0.01f, 2f)]
    private float farScaleMultiplier = 0.45f;

    [SerializeField]
    [Range(0f, 1f)]
    private float nearAlphaMultiplier = 0.5f;

    [SerializeField]
    [Range(0f, 1f)]
    private float farAlphaMultiplier = 0.15f;

    private Collider playerCollider;
    private Transform cameraTransform;
    private Vector3 baseProjectionScale;
    private Color baseProjectionColor;

    private void Awake()
    {
        playerCollider = GetComponent<Collider>();
        Camera mainCamera = Camera.main;
        cameraTransform = mainCamera != null ? mainCamera.transform : null;

        if (projectionRenderer == null)
        {
            Debug.LogError(
                $"PlayerGroundProjection on '{name}' requires a projection SpriteRenderer.");
            enabled = false;
            return;
        }

        if (groundLayers.value == 0)
        {
            Debug.LogWarning(
                $"PlayerGroundProjection on '{name}' has no Ground Layers configured.");
        }

        baseProjectionScale = projectionRenderer.transform.localScale;
        baseProjectionColor = projectionRenderer.color;
        SetProjectionVisible(false);
    }

    private void LateUpdate()
    {
        Vector3 origin = playerCollider.bounds.center;
        float rayDistance = maxProjectionDistance + playerCollider.bounds.extents.y;

        if (!Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                rayDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore))
        {
            SetProjectionVisible(false);
            return;
        }

        float airHeight = Mathf.Max(
            0f,
            hit.distance - playerCollider.bounds.extents.y);
        if (airHeight <= minimumAirHeight)
        {
            SetProjectionVisible(false);
            return;
        }

        UpdateProjection(hit, airHeight);
    }

    private void OnDisable()
    {
        SetProjectionVisible(false);
    }

    private void UpdateProjection(RaycastHit hit, float airHeight)
    {
        Transform projectionTransform = projectionRenderer.transform;
        if (faceCamera && cameraTransform != null)
        {
            projectionTransform.position =
                hit.point -
                cameraTransform.forward *
                Mathf.Max(surfaceOffset, cameraFacingDepthOffset);
            projectionTransform.forward = cameraTransform.forward;
        }
        else
        {
            projectionTransform.position = hit.point + hit.normal * surfaceOffset;
            projectionTransform.rotation = Quaternion.FromToRotation(
                Vector3.forward,
                hit.normal);
        }

        float heightRatio = Mathf.InverseLerp(
            minimumAirHeight,
            Mathf.Max(minimumAirHeight + 0.01f, fullFadeHeight),
            airHeight);
        float scaleMultiplier = Mathf.Lerp(
            nearScaleMultiplier,
            farScaleMultiplier,
            heightRatio);
        projectionTransform.localScale = baseProjectionScale * scaleMultiplier;

        Color color = baseProjectionColor;
        color.a = Mathf.Lerp(
            nearAlphaMultiplier,
            farAlphaMultiplier,
            heightRatio);
        projectionRenderer.color = color;
        SetProjectionVisible(true);
    }

    private void SetProjectionVisible(bool visible)
    {
        if (projectionRenderer != null && projectionRenderer.enabled != visible)
        {
            projectionRenderer.enabled = visible;
        }
    }
}
