using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveState : PlayerGroundedState
{
    public PlayerMoveState(Player player, PlayerStateMachine stateMachine, PlayerData playerData) : base(player, stateMachine, playerData)
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
            else
            {
                stateMachine.ChangeState(player.IdleState);
            }

            return;
        }

        float speed = playerData.movementVelocity;
        Vector3 moveDirection = player.GetMappedGroundMovement(new Vector2(xInput, yInput));
        player.SetVelocityXZ(moveDirection.x * speed, moveDirection.z * speed);

        if (xInput == 0 && yInput == 0)
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
}
