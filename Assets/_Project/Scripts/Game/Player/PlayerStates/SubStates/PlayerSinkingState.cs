using UnityEngine;

public sealed class PlayerSinkingState : PlayerState
{
    private Vector2 movementInput;
    private bool isGrounded;

    public PlayerSinkingState(Player player, PlayerStateMachine stateMachine, PlayerData playerData)
        : base(player, stateMachine, playerData)
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
        player.RB.angularVelocity = Vector3.zero;

        base.Exit();
    }
}
