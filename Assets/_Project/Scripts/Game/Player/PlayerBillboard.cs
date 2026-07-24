using UnityEngine;

/// <summary>
/// Makes the GameObject always face the main camera.
/// Used for 2D sprite quads in a 3D world with 45° fixed camera.
/// </summary>
public class PlayerBillboard : MonoBehaviour
{
    private Transform camTransform;

    private void Start()
    {
        camTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        transform.forward = camTransform.forward;
    }
}
