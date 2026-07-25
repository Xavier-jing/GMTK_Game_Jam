using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CameraZoneController : MonoBehaviour
{
    [SerializeField]
    private CinemachineVirtualCamera virtualCamera;

    [SerializeField]
    private CameraZoneSettings defaultSettings = CameraZoneSettings.Default;

    [SerializeField]
    private float blendSpeed = 4f;

    [SerializeField]
    private float exitDelay = 0.25f;

    [SerializeField]
    private bool applyDefaultOnStart = true;

    private readonly List<CameraZone> activeZones = new List<CameraZone>(4);
    private readonly List<PendingZoneExit> pendingExits = new List<PendingZoneExit>(4);
    private CameraZoneSettings targetSettings;
    private CinemachineFramingTransposer framingTransposer;
    private float manualOrthographicSizeOffset;

    private struct PendingZoneExit
    {
        public CameraZone Zone;
        public float Time;
    }

    private void Awake()
    {
        targetSettings = defaultSettings;
    }

    private void Start()
    {
        ResolveFramingTransposer();

        if (applyDefaultOnStart)
        {
            ApplySettings(defaultSettings);
        }
    }

    private void Update()
    {
        ProcessPendingExits();
        ResolveFramingTransposer();

        float t = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime);
        LensSettings lens = virtualCamera.m_Lens;
        lens.OrthographicSize = Mathf.Lerp(
            lens.OrthographicSize,
            targetSettings.orthographicSize + manualOrthographicSizeOffset,
            t);
        virtualCamera.m_Lens = lens;

        if (framingTransposer == null)
        {
            return;
        }

        framingTransposer.m_ScreenX = Mathf.Lerp(framingTransposer.m_ScreenX, targetSettings.screenX, t);
        framingTransposer.m_ScreenY = Mathf.Lerp(framingTransposer.m_ScreenY, targetSettings.screenY, t);
        framingTransposer.m_DeadZoneWidth = Mathf.Lerp(framingTransposer.m_DeadZoneWidth, targetSettings.deadZoneWidth, t);
        framingTransposer.m_DeadZoneHeight = Mathf.Lerp(framingTransposer.m_DeadZoneHeight, targetSettings.deadZoneHeight, t);
        framingTransposer.m_SoftZoneWidth = Mathf.Lerp(framingTransposer.m_SoftZoneWidth, targetSettings.softZoneWidth, t);
        framingTransposer.m_SoftZoneHeight = Mathf.Lerp(framingTransposer.m_SoftZoneHeight, targetSettings.softZoneHeight, t);
        framingTransposer.m_BiasX = Mathf.Lerp(framingTransposer.m_BiasX, targetSettings.biasX, t);
        framingTransposer.m_BiasY = Mathf.Lerp(framingTransposer.m_BiasY, targetSettings.biasY, t);

    }

    public float CurrentBaseOrthographicSize => targetSettings.orthographicSize;

    public void SetManualOrthographicSizeOffset(float offset)
    {
        manualOrthographicSizeOffset = offset;
    }

    public void EnterZone(CameraZone zone)
    {
        if (zone == null)
        {
            return;
        }

        RemovePendingExit(zone);

        if (!activeZones.Contains(zone))
        {
            activeZones.Add(zone);
        }

        RefreshTargetSettings();
    }

    public void ExitZone(CameraZone zone)
    {
        if (zone == null)
        {
            return;
        }

        if (!activeZones.Contains(zone))
        {
            return;
        }

        if (exitDelay <= 0f)
        {
            activeZones.Remove(zone);
            RefreshTargetSettings();
            return;
        }

        AddOrUpdatePendingExit(zone);
    }

    private void ProcessPendingExits()
    {
        for (int i = pendingExits.Count - 1; i >= 0; i--)
        {
            PendingZoneExit pendingExit = pendingExits[i];
            if (Time.time < pendingExit.Time)
            {
                continue;
            }

            activeZones.Remove(pendingExit.Zone);
            pendingExits.RemoveAt(i);
            RefreshTargetSettings();
        }
    }

    private void AddOrUpdatePendingExit(CameraZone zone)
    {
        float exitTime = Time.time + exitDelay;

        for (int i = 0; i < pendingExits.Count; i++)
        {
            if (pendingExits[i].Zone != zone)
            {
                continue;
            }

            pendingExits[i] = new PendingZoneExit
            {
                Zone = zone,
                Time = exitTime
            };
            return;
        }

        pendingExits.Add(new PendingZoneExit
        {
            Zone = zone,
            Time = exitTime
        });
    }

    private void RemovePendingExit(CameraZone zone)
    {
        for (int i = pendingExits.Count - 1; i >= 0; i--)
        {
            if (pendingExits[i].Zone == zone)
            {
                pendingExits.RemoveAt(i);
            }
        }
    }

    private void RefreshTargetSettings()
    {
        CameraZone selectedZone = null;

        for (int i = 0; i < activeZones.Count; i++)
        {
            CameraZone zone = activeZones[i];
            if (zone == null)
            {
                continue;
            }

            if (selectedZone == null || zone.Priority >= selectedZone.Priority)
            {
                selectedZone = zone;
            }
        }

        targetSettings = selectedZone != null ? selectedZone.Settings : defaultSettings;
    }

    private void ApplySettings(CameraZoneSettings settings)
    {
        LensSettings lens = virtualCamera.m_Lens;

        lens.OrthographicSize = settings.orthographicSize + manualOrthographicSizeOffset;

        virtualCamera.m_Lens = lens;

        framingTransposer.m_ScreenX = settings.screenX;

        framingTransposer.m_ScreenY = settings.screenY;

        framingTransposer.m_DeadZoneWidth = settings.deadZoneWidth;

        framingTransposer.m_DeadZoneHeight = settings.deadZoneHeight;

        framingTransposer.m_SoftZoneWidth = settings.softZoneWidth;

        framingTransposer.m_SoftZoneHeight = settings.softZoneHeight;

        framingTransposer.m_BiasX = settings.biasX;

        framingTransposer.m_BiasY = settings.biasY;
    }

    private void ResolveFramingTransposer()
    {
        framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
    }
}
