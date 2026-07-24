using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerAbilityState
{
    public PlayerJumpState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName) : base(player, stateMachine, playerData, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        int xInput = player.InputHandler.NormInputX;
        int yInput = player.InputHandler.NormInputY;
        float rad = playerData.movementAngle * Mathf.Deg2Rad;
        float worldX = xInput * Mathf.Cos(rad) - yInput * Mathf.Sin(rad);
        float worldZ = xInput * Mathf.Sin(rad) + yInput * Mathf.Cos(rad);

        player.CheckIfShouldFlip(xInput);
        player.SetVelocityXZ(worldX * playerData.movementVelocity, worldZ * playerData.movementVelocity);
        player.SetVelocityY(playerData.jumpVelocity);
        isAbilityDone = true;
    }


}
