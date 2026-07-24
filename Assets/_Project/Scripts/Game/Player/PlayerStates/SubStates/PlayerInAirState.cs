using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInAirState : PlayerAbilityState
{
    private bool isGrounded;
    private int xInput;
    private int yInput;
    public PlayerInAirState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName) : base(player, stateMachine, playerData, animBoolName)
    {
    }

    public override void DoChecks()
    {
        base.DoChecks();
        isGrounded = player.CheckIfTouchingGround();
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

        xInput = player.InputHandler.NormInputX;
        yInput = player.InputHandler.NormInputY;

        if (isGrounded && player.CurrentVelocity.y < 0.01f)
        {
            stateMachine.ChangeState(player.IdleState);
        }
        else
        {
            player.CheckIfShouldFlip(xInput);

            float rad = playerData.movementAngle * Mathf.Deg2Rad;
            float worldX = xInput * Mathf.Cos(rad) - yInput * Mathf.Sin(rad);
            float worldZ = xInput * Mathf.Sin(rad) + yInput * Mathf.Cos(rad);
            player.SetVelocityXZ(worldX * playerData.movementVelocity, worldZ * playerData.movementVelocity);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
}
