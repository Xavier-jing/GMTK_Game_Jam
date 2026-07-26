using UnityEngine;

public sealed class PlayerSinkingState : PlayerState
{
    private bool isGrounded;

    public PlayerSinkingState(Player player, PlayerStateMachine stateMachine, PlayerData playerData)
        : base(player, stateMachine, playerData)
    {
    }

    public override void Enter()
    {
        base.Enter();

        isGrounded = false;
        player.ReleaseRail();
        player.RB.isKinematic = false;
        player.RB.useGravity = true;
        player.SetVelocityXZ(0f, 0f);
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

        if (isGrounded && player.CurrentVelocity.y <= 0.01f)
        {
            player.GameplayStatus.SetCurrentLayer(PlayerWorldLayer.Lower);
            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Sinking is an automatic transition. Ground movement becomes available
        // only after the player reaches the lower layer while still carrying.
        player.SetVelocityXZ(0f, 0f);
    }

    public override void Exit()
    {
        player.RB.angularVelocity = Vector3.zero;

        base.Exit();
    }
}
