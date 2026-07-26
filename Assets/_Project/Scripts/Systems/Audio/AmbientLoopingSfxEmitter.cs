using UnityEngine;

[DisallowMultipleComponent]
public sealed class AmbientLoopingSfxEmitter : MonoBehaviour
{
    public enum AttenuationMode
    {
        Global2D = 0,
        PlayerDistance = 1,
    }

    [SerializeField] private string audioId;
    [SerializeField, Range(0f, 1f)] private float baseVolume = 1f;
    [SerializeField] private AttenuationMode attenuationMode = AttenuationMode.PlayerDistance;
    [SerializeField] private Transform distanceTarget;
    [SerializeField, Min(0f)] private float fullVolumeDistance = 2f;
    [SerializeField, Min(0.01f)] private float silentDistance = 10f;
    [SerializeField, Min(0.01f)] private float volumeChangeSpeed = 2f;

    private GameObject sourceRoot;
    private AudioSource source;
    private bool hasStarted;
    private bool initializationAttempted;
    private bool initializationSucceeded;
    private bool hasLoggedError;

    private void Awake()
    {
        sourceRoot = new GameObject("[Ambient Loop AudioSource]");
        sourceRoot.transform.SetParent(transform, false);
        source = sourceRoot.AddComponent<AudioSource>();
    }

    private void Start()
    {
        hasStarted = true;
        TryStartPlayback();
    }

    private void OnEnable()
    {
        if (hasStarted)
        {
            TryStartPlayback();
        }
    }

    private void Update()
    {
        if (!initializationSucceeded ||
            source == null ||
            attenuationMode != AttenuationMode.PlayerDistance)
        {
            return;
        }

        if (distanceTarget == null)
        {
            StopWithError("Distance Target is missing or was destroyed.");
            return;
        }

        float targetVolume = CalculateTargetVolume();
        source.volume = Mathf.MoveTowards(
            source.volume,
            targetVolume,
            volumeChangeSpeed * Time.unscaledDeltaTime);
    }

    private void OnDisable()
    {
        if (source != null)
        {
            source.Stop();
        }
    }

    private void OnDestroy()
    {
        if (source != null)
        {
            source.Stop();
        }

        if (sourceRoot != null)
        {
            Destroy(sourceRoot);
        }
    }

    private void OnValidate()
    {
        baseVolume = Mathf.Clamp01(baseVolume);
        fullVolumeDistance = Mathf.Max(0f, fullVolumeDistance);
        silentDistance = Mathf.Max(fullVolumeDistance + 0.01f, silentDistance);
        volumeChangeSpeed = Mathf.Max(0.01f, volumeChangeSpeed);
    }

    private void TryStartPlayback()
    {
        if (!TryInitialize())
        {
            return;
        }

        source.volume = CalculateTargetVolume();
        source.Play();
    }

    private bool TryInitialize()
    {
        if (initializationAttempted)
        {
            return initializationSucceeded;
        }

        initializationAttempted = true;

        if (source == null)
        {
            StopWithError("The dedicated AudioSource could not be created.");
            return false;
        }

        if (attenuationMode == AttenuationMode.PlayerDistance &&
            distanceTarget == null)
        {
            StopWithError("Distance Target is required in PlayerDistance mode.");
            return false;
        }

        if (silentDistance <= fullVolumeDistance)
        {
            StopWithError(
                "Silent Distance must be greater than Full Volume Distance.");
            return false;
        }

        if (!AppContext.HasInstance || AppContext.Instance.Audio == null)
        {
            StopWithError("AudioService is not available.");
            return false;
        }

        if (!AppContext.Instance.Audio.TryConfigureLoopingSfxById(
                source,
                audioId,
                out string error))
        {
            StopWithError(error);
            return false;
        }

        initializationSucceeded = true;
        return true;
    }

    private float CalculateTargetVolume()
    {
        if (attenuationMode == AttenuationMode.Global2D)
        {
            return baseVolume;
        }

        Vector3 emitterPosition = transform.position;
        Vector3 targetPosition = distanceTarget.position;
        float offsetX = emitterPosition.x - targetPosition.x;
        float offsetZ = emitterPosition.z - targetPosition.z;
        float distance = Mathf.Sqrt(offsetX * offsetX + offsetZ * offsetZ);

        if (distance <= fullVolumeDistance)
        {
            return baseVolume;
        }

        if (distance >= silentDistance)
        {
            return 0f;
        }

        float normalizedDistance = Mathf.InverseLerp(
            fullVolumeDistance,
            silentDistance,
            distance);
        float attenuation = 1f - Mathf.SmoothStep(0f, 1f, normalizedDistance);
        return baseVolume * attenuation;
    }

    private void StopWithError(string error)
    {
        initializationSucceeded = false;

        if (source != null)
        {
            source.Stop();
        }

        if (hasLoggedError)
        {
            return;
        }

        hasLoggedError = true;
        Debug.LogError(
            $"[AmbientLoopingSfxEmitter] '{name}' could not play AudioId " +
            $"'{audioId}': {error}",
            this);
    }
}
