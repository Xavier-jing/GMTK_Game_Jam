using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGameplayStatus))]
[RequireComponent(typeof(PlayerInteractionDetector))]
[RequireComponent(typeof(PlayerCarrySlot))]
[RequireComponent(typeof(PlayerInteractor))]
public class Player : MonoBehaviour
{
    #region State Variables
    public PlayerStateMachine stateMachine { get; private set; }

    public PlayerIdleState IdleState { get; private set; }
    public PlayerMoveState MoveState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerInAirState InAirState { get; private set; }
    public PlayerRailBoundState RailBoundState { get; private set; }
    public PlayerAscendState AscendState { get; private set; }
    public PlayerFloatingSwimState FloatingSwimState { get; private set; }
    public PlayerSinkingState SinkingState { get; private set; }
    public PlayerGameplayStatus GameplayStatus { get; private set; }
    public PlayerInteractionDetector InteractionDetector { get; private set; }
    public PlayerCarrySlot CarrySlot { get; private set; }
    public PlayerInteractor Interactor { get; private set; }

    [SerializeField]
    private PlayerData playerData;

    [SerializeField]
    private StraightRail initialRail;

    public StraightRail CurrentRail { get; private set; }
    public float RailDistance { get; private set; }

    #endregion

    #region Components
    public PlayerInputHandler InputHandler { get; private set; }
    public Rigidbody RB { get; private set; }
    public BoxCollider Collider3D { get; private set; }
    public Vector3 CurrentVelocity { get; private set; }
    public bool IsControlled { get; private set; }
    #endregion

    #region check Transforms
    [SerializeField]
    private Transform groundcheck;
    #endregion

    #region other variables
    public int FacingDirection { get; private set; }
    private Vector3 workspace;
    #endregion

    #region Callback Functions
    public void Awake()
    {
        stateMachine = new PlayerStateMachine();
        InputHandler = GetComponent<PlayerInputHandler>();
        RB = GetComponent<Rigidbody>();
        Collider3D = GetComponent<BoxCollider>();
        GameplayStatus = GetComponent<PlayerGameplayStatus>();
        InteractionDetector = GetComponent<PlayerInteractionDetector>();
        CarrySlot = GetComponent<PlayerCarrySlot>();
        if (CarrySlot == null)
        {
            CarrySlot = gameObject.AddComponent<PlayerCarrySlot>();
        }

        Interactor = GetComponent<PlayerInteractor>();
        if (Interactor == null)
        {
            Interactor = gameObject.AddComponent<PlayerInteractor>();
        }

        IdleState = new PlayerIdleState(this, stateMachine, playerData);
        MoveState = new PlayerMoveState(this, stateMachine, playerData);
        JumpState = new PlayerJumpState(this, stateMachine, playerData);
        InAirState = new PlayerInAirState(this, stateMachine, playerData);
        RailBoundState = new PlayerRailBoundState(this, stateMachine, playerData);
        AscendState = new PlayerAscendState(this, stateMachine, playerData);
        FloatingSwimState = new PlayerFloatingSwimState(this, stateMachine, playerData);
        SinkingState = new PlayerSinkingState(this, stateMachine, playerData);

        FacingDirection = 1;
        IsControlled = true;
    }

    private void Start()
    {
        if (initialRail != null)
        {
            BindToRail(initialRail);
            stateMachine.Initialize(RailBoundState);
        }
        else
        {
            stateMachine.Initialize(IdleState);
        }
    }

    private void Update()
    {
        CurrentVelocity = RB.velocity;
        stateMachine.currentState.LogicUpdate();
    }

    private void FixedUpdate()
    {
        stateMachine.currentState.PhysicsUpdate();
    }
    #endregion

    #region Set Functions
    public void SetVelocityXZ(float xVelocity, float zVelocity)
    {
        workspace.Set(xVelocity, CurrentVelocity.y, zVelocity);
        RB.velocity = workspace;
        CurrentVelocity = workspace;
    }

    public void SetVelocityY(float velocity)
    {
        workspace.Set(CurrentVelocity.x, velocity, CurrentVelocity.z);
        RB.velocity = workspace;
        CurrentVelocity = workspace;
    }

    public void BindToRail(StraightRail rail)
    {
        CurrentRail = rail;
        RailDistance = rail != null && RB != null
            ? rail.GetClosestDistance(RB.position)
            : 0f;
    }

    public void SetRailDistance(float distance)
    {
        RailDistance = CurrentRail != null
            ? CurrentRail.ClampDistance(distance)
            : 0f;
    }

    public void ReleaseRail()
    {
        CurrentRail = null;
        RailDistance = 0f;
    }

    // Called after rail removal interaction succeeds.
    public bool TryStartRailRemovedAscend()
    {
        if (!GameplayStatus.HasWrench)
        {
            return false;
        }

        GameplayStatus.MarkRailRemoved();
        return TryStartRiseToUpper();
    }

    // Shared upper-layer rise entry for rail removal and released floating items.
    public bool TryStartRiseToUpper()
    {
        if (!GameplayStatus.RailRemoved || GameplayStatus.HasSlotItem)
        {
            return false;
        }

        stateMachine.ChangeState(AscendState);
        return true;
    }


    // Called when the lower-layer interaction releases the carried floating item.
    public bool TryReleaseFloatingItemAndRise()
    {
        if (!GameplayStatus.RailRemoved ||
            !GameplayStatus.IsLowerLayer ||
            CarrySlot == null ||
            CarrySlot.CurrentProp == null)
        {
            return false;
        }

        // PlayerCarrySlot owns the carried world prop and must release it together
        // with the gameplay slot state. Clearing GameplayStatus directly would
        // leave an invisible/stale prop in the carry slot.
        return CarrySlot.TryDrop(CarrySlot.CurrentProp);
    }

    public bool TryStartCarryingSlotItem(PlayerSlotItemKind itemKind)
    {
        if (itemKind == PlayerSlotItemKind.None || GameplayStatus.HasSlotItem)
        {
            return false;
        }

        GameplayStatus.PutItemInSlot(itemKind);
        if (GameplayStatus.ShouldSink)
        {
            stateMachine.ChangeState(SinkingState);
        }

        return true;
    }

    public bool TryDropCarriedSlotItem(PlayerSlotItemKind expectedItemKind)
    {
        if (expectedItemKind == PlayerSlotItemKind.None ||
            GameplayStatus.SlotItemKind != expectedItemKind)
        {
            return false;
        }

        GameplayStatus.ClearItemSlot();
        if (GameplayStatus.ShouldRise)
        {
            stateMachine.ChangeState(AscendState);
        }

        return true;
    }

    /// <summary>
    /// Inverse-projects a screen-space input direction onto the world XZ ground plane.
    /// This compensates for the camera pitch, so equal screen X/Y input produces a
    /// visually correct 45-degree diagonal without adding any world Y movement.
    /// </summary>
    public Vector3 GetMappedGroundMovement(Vector2 input)
    {
        Vector2 screenInput = Vector2.ClampMagnitude(input, 1f);
        Camera movementCamera = Camera.main;
        if (movementCamera == null)
        {
            return new Vector3(screenInput.x, 0f, screenInput.y);
        }

        Vector3 groundRight = Vector3.ProjectOnPlane(
            movementCamera.transform.right,
            Vector3.up);
        Vector3 groundForward = Vector3.ProjectOnPlane(
            movementCamera.transform.forward,
            Vector3.up);

        if (groundRight.sqrMagnitude <= Mathf.Epsilon ||
            groundForward.sqrMagnitude <= Mathf.Epsilon)
        {
            return new Vector3(screenInput.x, 0f, screenInput.y);
        }

        groundRight.Normalize();
        groundForward.Normalize();

        float horizontalProjection = Mathf.Abs(Vector3.Dot(
            groundRight,
            movementCamera.transform.right));
        float verticalProjection = Mathf.Abs(Vector3.Dot(
            groundForward,
            movementCamera.transform.up));

        horizontalProjection = Mathf.Max(horizontalProjection, 0.0001f);
        verticalProjection = Mathf.Max(verticalProjection, 0.0001f);

        Vector3 mappedMovement =
            groundRight * (screenInput.x / horizontalProjection) +
            groundForward * (screenInput.y / verticalProjection);
        mappedMovement.y = 0f;

        return mappedMovement;
    }

    public Vector3 GetCameraRelativeMovement(Vector2 input)
    {
        Camera movementCamera = Camera.main;
        if (movementCamera == null)
        {
            return new Vector3(input.x, 0f, input.y);
        }

        Vector3 forward = movementCamera.transform.forward;
        Vector3 right = movementCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return right * input.x + forward * input.y;
    }
    #endregion

    #region Check Functions
    public bool CheckIfTouchingGround()
    {
        return Physics.OverlapSphere(groundcheck.position, playerData.groundCheckRadius, playerData.whatIsGround).Length > 0;
    }
    #endregion

    #region other Functions

    public void SetControlled(bool controlled)
    {
        IsControlled = controlled;

        if (controlled)
        {
            InputHandler.EnableGameplayInput();
        }
        else
        {
            InputHandler.DisableGameplayInput();
        }
    }
    #endregion
}
