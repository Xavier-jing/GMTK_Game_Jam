using System;
using UnityEngine;

[Serializable]
public struct CameraZoneSettings
{
    [Header("Lens")]
    public float orthographicSize;

    [Header("Framing")]
    [Range(0f, 1f)]
    public float screenX;

    [Range(0f, 1f)]
    public float screenY;

    [Range(0f, 1f)]
    public float deadZoneWidth;

    [Range(0f, 1f)]
    public float deadZoneHeight;

    [Range(0f, 2f)]
    public float softZoneWidth;

    [Range(0f, 2f)]
    public float softZoneHeight;

    [Range(-1f, 1f)]
    public float biasX;

    [Range(-1f, 1f)]
    public float biasY;

    public static CameraZoneSettings Default => new CameraZoneSettings
    {
        orthographicSize = 8f,
        screenX = 0.5f,
        screenY = 0.45f,
        deadZoneWidth = 0.6f,
        deadZoneHeight = 0.5f,
        softZoneWidth = 0.9f,
        softZoneHeight = 0.8f,
        biasX = 0f,
        biasY = 0f
    };
}
