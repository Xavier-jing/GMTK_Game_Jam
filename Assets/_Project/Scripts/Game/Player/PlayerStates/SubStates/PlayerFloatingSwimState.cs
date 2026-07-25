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

        movementInput = new Vector2(
            player.InputHandler.NormInputX,
            player.InputHandler.NormInputY);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        Vector3 moveDirection = player.GetMappedGroundMovement(movementInput);

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
