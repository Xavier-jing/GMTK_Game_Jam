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

        Vector3 moveDirection = player.GetMappedGroundMovement(movementInput);

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
