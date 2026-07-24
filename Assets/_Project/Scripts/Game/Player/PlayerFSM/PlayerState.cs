using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState
{
    //引用玩家
    protected Player player;
    protected PlayerStateMachine stateMachine;
    protected PlayerData playerData;

    private string animBoolName;
    protected float startTime;

    public PlayerState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
    {
        this.player = player;
        this.stateMachine = stateMachine;
        this.playerData = playerData;
        this.animBoolName = animBoolName;
    }//构函

    public virtual void Enter()
    {
        DoChecks();
        player.SetAnimationBool(animBoolName, true);
        startTime = Time.time;
    }

    public virtual void Exit()
    {
        player.SetAnimationBool(animBoolName, false);
    }

    public virtual void LogicUpdate()
    {

    }

    public virtual void PhysicsUpdate()
    {
        DoChecks();
    }

    public virtual void DoChecks() //检测
    {

    }
}
