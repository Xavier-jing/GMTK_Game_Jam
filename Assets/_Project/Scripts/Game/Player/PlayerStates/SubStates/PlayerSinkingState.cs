using UnityEngine;

public sealed class PlayerSinkingState : PlayerState
{
    private Vector2 movementInput;
    private bool isGrounded;
    private bool isMoving;

    public PlayerSinkingState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
        : base(player, stateMachine, playerData, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        movementInput = Vector2.zero;
        isGrounded = false;
        player.ReleaseRail();
        player.RB.isKinematic = false;
        player.RB.useGravity = true;
        player.RB.angularVelocity = Vector3.zero;
        isMoving = true;
        SetMovementAnimation(false);
    }

    public override void DoChecks()
    {
        base.DoChecks();

        isGrounded = player.CheckIfTouchingGround();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        movementInput = player.InputHandler.RawMovementInput;
        bool moving = movementInput.sqrMagnitude > 0.01f;

        if (moving)
        {
            player.CheckIfShouldFlip(movementInput.x > 0f ? 1 : -1);
        }

        SetMovementAnimation(moving);

        if (isGrounded && player.CurrentVelocity.y <= 0.01f)
        {
            player.GameplayStatus.SetCurrentLayer(PlayerWorldLayer.Lower);
            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        float rad = playerData.movementAngle * Mathf.Deg2Rad;
        float worldX = movementInput.x * Mathf.Cos(rad) - movementInput.y * Mathf.Sin(rad);
        float worldZ = movementInput.x * Mathf.Sin(rad) + movementInput.y * Mathf.Cos(rad);
        Vector3 moveDirection = new Vector3(worldX, 0f, worldZ);

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        player.SetVelocityXZ(
            moveDirection.x * playerData.sinkingMovementVelocity,
            moveDirection.z * playerData.sinkingMovementVelocity);
    }

    public override void Exit()
    {
        SetMovementAnimation(false);
        player.RB.angularVelocity = Vector3.zero;

        base.Exit();
    }

    private void SetMovementAnimation(bool moving)
    {
        if (isMoving == moving)
        {
            return;
        }

        isMoving = moving;
        player.SetAnimationBool("idle", !moving);
        player.SetAnimationBool("move", moving);
    }
}
