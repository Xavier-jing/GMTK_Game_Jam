using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerAbilityState
{
    public PlayerJumpState(Player player, PlayerStateMachine stateMachine, PlayerData playerData) : base(player, stateMachine, playerData)
    {
    }

    public override void Enter()
    {
        base.Enter();

        int xInput = player.InputHandler.NormInputX;
        int yInput = player.InputHandler.NormInputY;
        Vector3 moveDirection = player.GetMappedGroundMovement(new Vector2(xInput, yInput));
        player.SetVelocityXZ(
            moveDirection.x * playerData.movementVelocity,
            moveDirection.z * playerData.movementVelocity);
        player.SetVelocityY(playerData.jumpVelocity);
        isAbilityDone = true;
    }


}
