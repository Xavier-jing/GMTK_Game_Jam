using UnityEngine;

public sealed class PlayerFloatingSwimState : PlayerState
{
    private Vector2 movementInput;
    private bool isMoving;

    public PlayerFloatingSwimState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
        : base(player, stateMachine, playerData, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        movementInput = Vector2.zero;
        player.ReleaseRail();
        player.RB.isKinematic = false;
        player.RB.useGravity = false;
        player.RB.velocity = Vector3.zero;
        player.RB.angularVelocity = Vector3.zero;
        isMoving = true;
        SetMovementAnimation(false);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (player.GameplayStatus.ShouldSink)
        {
            stateMachine.ChangeState(player.SinkingState);
            return;
        }

        movementInput = player.InputHandler.RawMovementInput;
        bool moving = movementInput.sqrMagnitude > 0.01f;

        if (moving)
        {
            player.CheckIfShouldFlip(movementInput.x > 0f ? 1 : -1);
        }

        SetMovementAnimation(moving);
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
            moveDirection.x * playerData.floatingSwimVelocity,
            moveDirection.z * playerData.floatingSwimVelocity);
        player.SetVelocityY(0f);
    }

    public override void Exit()
    {
        SetMovementAnimation(false);
        player.RB.velocity = Vector3.zero;
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
