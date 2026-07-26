using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerGroundedState
{
    public PlayerIdleState(Player player, PlayerStateMachine stateMachine, PlayerData playerData) : base(player, stateMachine, playerData)
    {
    }

    public override void DoChecks()
    {
        base.DoChecks();
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
        player.SetVelocityXZ(0f, 0f);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (stateMachine.currentState != this)
        {
            return;
        }

        if (!player.GameplayStatus.CanUseWeightedGroundMovement)
        {
            player.SetVelocityXZ(0f, 0f);

            if (player.GameplayStatus.ShouldRise)
            {
                player.TryStartRiseToUpper();
            }

            return;
        }

        if (xInput != 0 || yInput != 0)
        {
            stateMachine.ChangeState(player.MoveState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
}
