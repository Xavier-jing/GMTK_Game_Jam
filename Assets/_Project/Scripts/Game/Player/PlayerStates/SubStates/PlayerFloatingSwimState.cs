using UnityEngine;

public sealed class PlayerFloatingSwimState : PlayerState
{
    private Vector2 movementInput;

    public PlayerFloatingSwimState(Player player, PlayerStateMachine stateMachine, PlayerData playerData)
        : base(player, stateMachine, playerData)
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
        player.RB.velocity = Vector3.zero;
        player.RB.angularVelocity = Vector3.zero;

        base.Exit();
    }
}
