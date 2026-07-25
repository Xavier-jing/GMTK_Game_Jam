using UnityEngine;

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(PlayerInputHandler))]
public sealed class PlayerAnimationController : MonoBehaviour
{
    private static readonly int IdleHash = Animator.StringToHash("idle");
    private static readonly int MoveHash = Animator.StringToHash("move");
    private static readonly int InAirHash = Animator.StringToHash("inair");
    private static readonly int MoveXHash = Animator.StringToHash("moveX");
    private static readonly int MoveYHash = Animator.StringToHash("moveY");
    private static readonly int FacingXHash = Animator.StringToHash("facingX");
    private static readonly int FacingYHash = Animator.StringToHash("facingY");

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private float moveInputDeadZone = 0.01f;

    private Player player;
    private PlayerInputHandler inputHandler;
    private Vector2 lastMoveDirection = new Vector2(1f, -1f);

    private void Awake()
    {
        player = GetComponent<Player>();
        inputHandler = GetComponent<PlayerInputHandler>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (animator == null ||
            player == null ||
            inputHandler == null ||
            player.stateMachine.currentState == null)
        {
            return;
        }

        Vector2 moveInput = inputHandler.RawMovementInput;
        bool isMoving = moveInput.sqrMagnitude > moveInputDeadZone;
        bool isInAir = IsAirState(player.stateMachine.currentState);

        if (isMoving)
        {
            lastMoveDirection = GetIsometricFacingDirection(moveInput);
        }

        // Write direction before state bools so transitions enter blend trees with the current facing.
        SetFloatIfExists(MoveXHash, lastMoveDirection.x);
        SetFloatIfExists(MoveYHash, lastMoveDirection.y);
        SetFloatIfExists(FacingXHash, lastMoveDirection.x);
        SetFloatIfExists(FacingYHash, lastMoveDirection.y);
        SetBoolIfExists(IdleHash, !isMoving && !isInAir);
        SetBoolIfExists(MoveHash, isMoving && !isInAir);
        SetBoolIfExists(InAirHash, isInAir);
    }

    private static Vector2 GetIsometricFacingDirection(Vector2 input)
    {
        float facingX = input.x - input.y;
        float facingY = input.x + input.y;

        return new Vector2(
            facingX < 0f ? -1f : 1f,
            facingY < 0f ? -1f : 1f);
    }

    private static bool IsAirState(PlayerState state)
    {
        return state is PlayerJumpState ||
               state is PlayerInAirState ||
               state is PlayerAscendState ||
               state is PlayerFloatingSwimState ||
               state is PlayerSinkingState;
    }

    private void SetBoolIfExists(int parameterHash, bool value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash &&
                parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterHash, value);
                return;
            }
        }
    }

    private void SetFloatIfExists(int parameterHash, float value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash &&
                parameter.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(parameterHash, value);
                return;
            }
        }
    }
}
