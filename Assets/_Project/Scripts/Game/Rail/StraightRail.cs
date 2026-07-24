using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class StraightRail : MonoBehaviour
{
    private const float MinimumSegmentLength = 0.0001f;

    [Header("Polyline Path")]
    [SerializeField]
    [Tooltip("Ordered points that form one continuous rail. When fewer than two are assigned, the BoxCollider endpoints are used.")]
    private List<Transform> controlPoints = new List<Transform>();

    [SerializeField]
    private BoxCollider railArea;

    [SerializeField]
    private Vector3 pathOffset;

    [SerializeField]
    [Min(0.01f)]
    private float gizmoPointRadius = 0.12f;

    public BoxCollider RailArea => railArea;
    public Vector3 StartPosition => GetPathPoint(0);
    public Vector3 EndPosition => GetPathPoint(PathPointCount - 1);
    public Vector3 Direction => GetDirection(0f);
    public float Length => CalculateLength();

    private bool UsesControlPoints => controlPoints != null && controlPoints.Count >= 2;
    private int PathPointCount => UsesControlPoints ? controlPoints.Count : 2;

    private void Reset()
    {
        railArea = GetComponent<BoxCollider>();
        railArea.isTrigger = true;
    }

    private void OnValidate()
    {
        if (railArea != null)
        {
            railArea.isTrigger = true;
        }
    }

    public float ClampDistance(float distance)
    {
        return Mathf.Clamp(distance, 0f, Length);
    }

    public Vector3 GetPosition(float distance)
    {
        float remainingDistance = ClampDistance(distance);

        for (int i = 0; i < PathPointCount - 1; i++)
        {
            Vector3 start = GetPathPoint(i);
            Vector3 end = GetPathPoint(i + 1);
            float segmentLength = Vector3.Distance(start, end);

            if (segmentLength <= MinimumSegmentLength)
            {
                continue;
            }

            if (remainingDistance <= segmentLength)
            {
                return Vector3.Lerp(start, end, remainingDistance / segmentLength);
            }

            remainingDistance -= segmentLength;
        }

        return EndPosition;
    }

    public Vector3 GetDirection(float distance)
    {
        float remainingDistance = ClampDistance(distance);

        for (int i = 0; i < PathPointCount - 1; i++)
        {
            Vector3 segment = GetPathPoint(i + 1) - GetPathPoint(i);
            float segmentLength = segment.magnitude;

            if (segmentLength <= MinimumSegmentLength)
            {
                continue;
            }

            if (remainingDistance < segmentLength || i == PathPointCount - 2)
            {
                return segment / segmentLength;
            }

            remainingDistance -= segmentLength;
        }

        return transform.right;
    }

    public float GetClosestDistance(Vector3 worldPosition)
    {
        float closestSqrDistance = float.PositiveInfinity;
        float closestPathDistance = 0f;
        float distanceBeforeSegment = 0f;

        for (int i = 0; i < PathPointCount - 1; i++)
        {
            Vector3 start = GetPathPoint(i);
            Vector3 segment = GetPathPoint(i + 1) - start;
            float segmentLength = segment.magnitude;

            if (segmentLength <= MinimumSegmentLength)
            {
                continue;
            }

            Vector3 direction = segment / segmentLength;
            float distanceOnSegment = Mathf.Clamp(
                Vector3.Dot(worldPosition - start, direction),
                0f,
                segmentLength);
            Vector3 closestPoint = start + direction * distanceOnSegment;
            float sqrDistance = (worldPosition - closestPoint).sqrMagnitude;

            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestPathDistance = distanceBeforeSegment + distanceOnSegment;
            }

            distanceBeforeSegment += segmentLength;
        }

        return ClampDistance(closestPathDistance);
    }

    private Vector3 GetLocalEndpoint(float direction)
    {
        Vector3 endpoint = railArea != null ? railArea.center : Vector3.zero;
        float halfLength = railArea != null ? railArea.size.x * 0.5f : 0.5f;
        endpoint.x += halfLength * direction;
        return endpoint + pathOffset;
    }

    private Vector3 GetPathPoint(int index)
    {
        if (!UsesControlPoints)
        {
            return transform.TransformPoint(GetLocalEndpoint(index == 0 ? -1f : 1f));
        }

        Transform point = controlPoints[index];
        if (point == null)
        {
            return transform.position + transform.TransformVector(pathOffset);
        }

        return point.position + transform.TransformVector(pathOffset);
    }

    private float CalculateLength()
    {
        float totalLength = 0f;

        for (int i = 0; i < PathPointCount - 1; i++)
        {
            totalLength += Vector3.Distance(GetPathPoint(i), GetPathPoint(i + 1));
        }

        return totalLength;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        for (int i = 0; i < PathPointCount; i++)
        {
            Vector3 point = GetPathPoint(i);
            Gizmos.DrawSphere(point, gizmoPointRadius);

            if (i < PathPointCount - 1)
            {
                Gizmos.DrawLine(point, GetPathPoint(i + 1));
            }
        }
    }
}
