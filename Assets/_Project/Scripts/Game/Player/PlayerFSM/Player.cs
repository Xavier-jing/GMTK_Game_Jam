using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGameplayStatus))]
[RequireComponent(typeof(PlayerInteractionDetector))]
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

    [SerializeField]
    private PlayerData playerData;

    [SerializeField]
    private StraightRail initialRail;

    public StraightRail CurrentRail { get; private set; }
    public float RailDistance { get; private set; }

    #endregion

    #region Components
    public Animator Anim { get; private set; }
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

        Anim = GetComponentInChildren<Animator>();
        InputHandler = GetComponent<PlayerInputHandler>();
        RB = GetComponent<Rigidbody>();
        Collider3D = GetComponent<BoxCollider>();
        GameplayStatus = GetComponent<PlayerGameplayStatus>();
        InteractionDetector = GetComponent<PlayerInteractionDetector>();

        IdleState = new PlayerIdleState(this, stateMachine, playerData, "idle");
        MoveState = new PlayerMoveState(this, stateMachine, playerData, "move");
        JumpState = new PlayerJumpState(this, stateMachine, playerData, "inair");
        InAirState = new PlayerInAirState(this, stateMachine, playerData, "inair");
        RailBoundState = new PlayerRailBoundState(this, stateMachine, playerData);
        AscendState = new PlayerAscendState(this, stateMachine, playerData, "inair");
        FloatingSwimState = new PlayerFloatingSwimState(this, stateMachine, playerData, "inair");
        SinkingState = new PlayerSinkingState(this, stateMachine, playerData, "inair");

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
        if (!GameplayStatus.RailRemoved || !GameplayStatus.IsLowerLayer || !GameplayStatus.HasFloatingSmallItem)
        {
            return false;
        }

        GameplayStatus.ClearItemSlot();
        return TryStartRiseToUpper();
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

    public void SetAnimationBool(string parameterName, bool value)
    {
        int parameterHash = Animator.StringToHash(parameterName);
        foreach (AnimatorControllerParameter parameter in Anim.parameters)
        {
            if (parameter.nameHash == parameterHash &&
                parameter.type == AnimatorControllerParameterType.Bool)
            {
                Anim.SetBool(parameterHash, value);
                return;
            }
        }
    }
    #endregion

    #region Check Functions
    public bool CheckIfTouchingGround()
    {
        return Physics.OverlapSphere(groundcheck.position, playerData.groundCheckRadius, playerData.whatIsGround).Length > 0;
    }

    public void CheckIfShouldFlip(int xInput)
    {
        if (xInput != 0 && xInput != FacingDirection)
        {
            Flip();
        }
    }
    #endregion

    #region other Functions
    private void Flip()
    {
        FacingDirection *= -1;
        transform.Rotate(0.0f, 180.0f, 0.0f);
    }

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
