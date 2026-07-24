using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newPlayerData", menuName = "Data/Player Data/Base Data")]
public class PlayerData : ScriptableObject
{
    [Header("Rail State")]
    public float railMovementVelocity = 4f;

    [Header("Move State")]
    public float movementVelocity = 5f;

    [Header("Rotate Angle")]
    public float movementAngle = 0f;

    [Header("Jump State")]
    public float jumpVelocity = 6f;

    [Header("Ascend State")]
    public float ascendVelocity = 2.5f;
    public float upperLayerHeight = 4f;
    public float upperLayerSnapDistance = 0.05f;

    [Header("Floating Swim State")]
    public float floatingSwimVelocity = 4f;

    [Header("Sinking State")]
    public float sinkingMovementVelocity = 3f;

    [Header("Check Variables")]
    public float groundCheckRadius = 0.3f;
    public LayerMask whatIsGround;
}
