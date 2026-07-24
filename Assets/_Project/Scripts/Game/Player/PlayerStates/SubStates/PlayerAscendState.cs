using UnityEngine;

public sealed class PlayerAscendState : PlayerState
{
    private bool reachedUpperLayer;

    public PlayerAscendState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
        : base(player, stateMachine, playerData, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        reachedUpperLayer = false;
        player.ReleaseRail();
        player.RB.isKinematic = false;
        player.RB.useGravity = false;
        player.RB.velocity = Vector3.zero;
        player.RB.angularVelocity = Vector3.zero;
        player.SetVelocityY(playerData.ascendVelocity);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (reachedUpperLayer)
        {
            return;
        }

        if (player.transform.position.y >= playerData.upperLayerHeight - playerData.upperLayerSnapDistance)
        {
            reachedUpperLayer = true;
            player.GameplayStatus.SetCurrentLayer(PlayerWorldLayer.Upper);
            player.RB.position = new Vector3(
                player.RB.position.x,
                playerData.upperLayerHeight,
                player.RB.position.z);
            player.RB.velocity = Vector3.zero;
            player.RB.angularVelocity = Vector3.zero;
            player.SetAnimationBool("idle", true);

            if (player.GameplayStatus.CanFloatSwim)
            {
                stateMachine.ChangeState(player.FloatingSwimState);
            }
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (reachedUpperLayer)
        {
            return;
        }

        player.SetVelocityY(playerData.ascendVelocity);
    }

    public override void Exit()
    {
        player.SetAnimationBool("idle", false);
        base.Exit();
    }
}
