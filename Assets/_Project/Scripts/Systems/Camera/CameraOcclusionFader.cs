using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CameraOcclusionFader : MonoBehaviour
{
    private const int MaxHits = 32;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");

    [Header("References")]
    [SerializeField]
    private Camera sourceCamera;

    [SerializeField]
    private Transform target;

    [SerializeField]
    private Vector3 targetOffset = new Vector3(0f, 1f, 0f);

    [Header("Occlusion Detection")]
    [SerializeField]
    private LayerMask occluderLayers;

    [SerializeField]
    [Min(0f)]
    private float castRadius = 0.25f;

    [Header("Fade")]
    [SerializeField]
    [Range(0f, 1f)]
    private float fadedAlpha = 0.35f;

    [SerializeField]
    [Min(0.01f)]
    private float fadeSpeed = 4f;

    private readonly RaycastHit[] hits = new RaycastHit[MaxHits];
    private readonly Dictionary<Renderer, FadeState> fadeStates =
        new Dictionary<Renderer, FadeState>();
    private readonly HashSet<Renderer> currentOccluders = new HashSet<Renderer>();
    private readonly List<Renderer> stateKeys = new List<Renderer>();
    private bool hasWarnedAboutHitLimit;

    private void Awake()
    {
        if (sourceCamera == null)
        {
            sourceCamera = Camera.main;
        }

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform : null;
        }

        if (sourceCamera == null || target == null)
        {
            Debug.LogError(
                $"CameraOcclusionFader on '{name}' requires a source camera and target.");
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        FindCurrentOccluders();
        UpdateFadeStates();
    }

    private void OnDisable()
    {
        RestoreAllRenderers();
    }

    private void OnDestroy()
    {
        RestoreAllRenderers();
    }

    private void FindCurrentOccluders()
    {
        currentOccluders.Clear();

        Vector3 origin = sourceCamera.transform.position;
        Vector3 destination = target.position + targetOffset;
        Vector3 offset = destination - origin;
        float distance = offset.magnitude;
        if (distance <= Mathf.Epsilon)
        {
            return;
        }

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            castRadius,
            offset / distance,
            hits,
            distance,
            occluderLayers,
            QueryTriggerInteraction.Ignore);

        if (hitCount == MaxHits && !hasWarnedAboutHitLimit)
        {
            hasWarnedAboutHitLimit = true;
            Debug.LogWarning(
                $"CameraOcclusionFader on '{name}' reached its {MaxHits}-hit limit. " +
                "Reduce the occluder layer scope or cast radius.");
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || IsPartOfTarget(hitCollider.transform))
            {
                continue;
            }

            Renderer hitRenderer = ResolveRenderer(hitCollider);
            if (hitRenderer != null)
            {
                currentOccluders.Add(hitRenderer);
            }
        }
    }

    private void UpdateFadeStates()
    {
        foreach (Renderer renderer in currentOccluders)
        {
            if (!fadeStates.ContainsKey(renderer))
            {
                FadeState newState = new FadeState(renderer);
                fadeStates.Add(renderer, newState);

                if (newState.HasOpaqueUrpMaterial)
                {
                    Debug.LogWarning(
                        $"Occluder '{renderer.name}' uses an opaque material. " +
                        "Its shader must be configured for Transparent surface rendering " +
                        "before alpha fading will be visible.",
                        renderer);
                }
            }
        }

        stateKeys.Clear();
        stateKeys.AddRange(fadeStates.Keys);

        float maxDelta = fadeSpeed * Time.unscaledDeltaTime;
        for (int i = 0; i < stateKeys.Count; i++)
        {
            Renderer renderer = stateKeys[i];
            FadeState state = fadeStates[renderer];

            if (renderer == null)
            {
                state.Dispose();
                fadeStates.Remove(renderer);
                continue;
            }

            float targetAlpha = currentOccluders.Contains(renderer) ? fadedAlpha : 1f;
            state.CurrentAlpha = Mathf.MoveTowards(
                state.CurrentAlpha,
                targetAlpha,
                maxDelta);
            state.ApplyAlpha();

            if (targetAlpha >= 1f && Mathf.Approximately(state.CurrentAlpha, 1f))
            {
                state.RestoreRenderer();
                fadeStates.Remove(renderer);
            }
        }
    }

    private bool IsPartOfTarget(Transform candidate)
    {
        return candidate == target ||
               candidate.IsChildOf(target) ||
               target.IsChildOf(candidate);
    }

    private static Renderer ResolveRenderer(Collider hitCollider)
    {
        Renderer renderer = hitCollider.GetComponentInParent<Renderer>();
        if (renderer != null)
        {
            return renderer;
        }

        return hitCollider.GetComponentInChildren<Renderer>();
    }

    private void RestoreAllRenderers()
    {
        foreach (FadeState state in fadeStates.Values)
        {
            state.RestoreRenderer();
        }

        fadeStates.Clear();
        currentOccluders.Clear();
        stateKeys.Clear();
    }

    private sealed class FadeState
    {
        private readonly Renderer renderer;
        private readonly Material[] originalMaterials;
        private readonly Material[] runtimeMaterials;
        private readonly int[] colorPropertyIds;
        private readonly Color[] originalColors;
        private bool isRestored;

        public FadeState(Renderer renderer)
        {
            this.renderer = renderer;
            originalMaterials = renderer.sharedMaterials;
            runtimeMaterials = new Material[originalMaterials.Length];
            colorPropertyIds = new int[originalMaterials.Length];
            originalColors = new Color[originalMaterials.Length];

            for (int i = 0; i < originalMaterials.Length; i++)
            {
                Material original = originalMaterials[i];
                if (original == null)
                {
                    continue;
                }

                Material runtime = new Material(original)
                {
                    name = original.name + " (Camera Occlusion Runtime)"
                };
                runtimeMaterials[i] = runtime;

                int colorPropertyId = ResolveColorProperty(runtime);
                colorPropertyIds[i] = colorPropertyId;
                if (colorPropertyId != 0)
                {
                    originalColors[i] = runtime.GetColor(colorPropertyId);
                }

                if (runtime.HasProperty(SurfaceId) && runtime.GetFloat(SurfaceId) < 0.5f)
                {
                    HasOpaqueUrpMaterial = true;
                }
            }

            renderer.sharedMaterials = runtimeMaterials;
        }

        public float CurrentAlpha { get; set; } = 1f;
        public bool HasOpaqueUrpMaterial { get; }

        public void ApplyAlpha()
        {
            for (int i = 0; i < runtimeMaterials.Length; i++)
            {
                Material material = runtimeMaterials[i];
                int colorPropertyId = colorPropertyIds[i];
                if (material == null || colorPropertyId == 0)
                {
                    continue;
                }

                Color color = originalColors[i];
                color.a *= CurrentAlpha;
                material.SetColor(colorPropertyId, color);
            }
        }

        public void RestoreRenderer()
        {
            if (isRestored)
            {
                return;
            }

            isRestored = true;
            if (renderer != null)
            {
                renderer.sharedMaterials = originalMaterials;
            }

            Dispose();
        }

        public void Dispose()
        {
            for (int i = 0; i < runtimeMaterials.Length; i++)
            {
                Material material = runtimeMaterials[i];
                if (material != null)
                {
                    Object.Destroy(material);
                }
            }
        }

        private static int ResolveColorProperty(Material material)
        {
            if (material.HasProperty(BaseColorId))
            {
                return BaseColorId;
            }

            return material.HasProperty(ColorId) ? ColorId : 0;
        }
    }
}
