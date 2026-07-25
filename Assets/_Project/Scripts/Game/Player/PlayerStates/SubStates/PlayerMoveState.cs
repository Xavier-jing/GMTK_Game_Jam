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

        float speed = playerData.movementVelocity;
        float rad = playerData.movementAngle * Mathf.Deg2Rad;
        float worldX = xInput * Mathf.Cos(rad) - yInput * Mathf.Sin(rad);
        float worldZ = xInput * Mathf.Sin(rad) + yInput * Mathf.Cos(rad);
        player.SetVelocityXZ(worldX * speed, worldZ * speed);

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
