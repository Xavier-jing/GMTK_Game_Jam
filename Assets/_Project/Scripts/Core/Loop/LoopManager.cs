using System;
using UnityEngine.SceneManagement;

public sealed class LoopManager : IDisposable
{
    private readonly TurnManager turnManager;
    private readonly Inventory inventory;
    private readonly SceneLoader sceneLoader;
    private readonly LoopProgress loopProgress;
    private readonly RunState runState;

    public event Action<int> RunStarted;
    public event Action<RunEndReason, int> RunEnded;

    public int CurrentRun => loopProgress.CurrentLoop;

    public bool IsEndingRun { get; private set; }

    public LoopManager(TurnManager turnManager,Inventory inventory,SceneLoader sceneLoader,
    LoopProgress loopProgress,RunState runState)
    {
        this.turnManager = turnManager;
        this.inventory = inventory;
        this.sceneLoader = sceneLoader;
        this.loopProgress = loopProgress;
        this.runState = runState;
    }

    public void Dispose()
    {
        
    }

    //结束当前运行
    public void EndRun(RunEndReason reason)
    {
        if (!CanEndRun(reason))
        {
            return;
        }

        IsEndingRun = true;
        ApplyPermanentProgress(reason);
        RunEnded?.Invoke(reason, CurrentRun);
        StartNextRun();
    }

    //判断当前能否以 reason 结束运行
    public bool CanEndRun(RunEndReason reason)
    {
        if (IsEndingRun)
        {
            return false;
        }

        bool isTruthEnding =
            reason == RunEndReason.EndingTwo ||
            reason == RunEndReason.EndingThree;

        return !isTruthEnding || loopProgress.TruthKnown;
    }

    ///正式开始下一次循环
    public void StartNextRun()
    {
        loopProgress.StartNextLoop();
        inventory.Clear();
        runState.Reset();
        turnManager.ResetTurns();
        IsEndingRun = false;
        RunStarted?.Invoke(CurrentRun);

        if (TryGetReloadTarget(out SceneId sceneId))
        {
            sceneLoader.LoadScene(sceneId, LoadSceneMode.Single);
        }
    }

    //根据结束原因更新跨循环的永久数据
    private void ApplyPermanentProgress(RunEndReason reason)
    {
        switch (reason)
        {
            case RunEndReason.TruthRevealed:
                loopProgress.RevealTruth();
                break;

            case RunEndReason.EndingTwo:
                loopProgress.MarkEndingTwoReached();
                break;

            case RunEndReason.EndingThree:
                loopProgress.MarkEndingThreeReached();
                break;
        }
    }

    //获取需要重新加载的场景ID
    private bool TryGetReloadTarget(out SceneId sceneId)
    {
        if (sceneLoader.IsActiveScene(SceneId.Gameplay))
        {
            sceneId = SceneId.Gameplay;
            return true;
        }

        if (sceneLoader.IsActiveScene(SceneId.Sandbox))
        {
            sceneId = SceneId.Sandbox;
            return true;
        }

        sceneId = default;
        return false;
    }
}
