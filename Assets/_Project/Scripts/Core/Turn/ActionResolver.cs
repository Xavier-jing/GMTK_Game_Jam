using System;

public sealed class ActionResolver
{
    private readonly TurnManager turnManager;
    private readonly LoopManager loopManager;

    public ActionResolver(TurnManager turnManager, LoopManager loopManager)
    {
        this.turnManager = turnManager;
        this.loopManager = loopManager;
    }

    public bool Resolve(int turnDelta, Action execute, RunEndReason? immediateEndReason = null)
    {
        if (loopManager.IsEndingRun)
        {
            return false;
        }

        if (immediateEndReason.HasValue &&
            !loopManager.CanEndRun(immediateEndReason.Value))
        {
            return false;
        }

        execute?.Invoke();
        turnManager.ChangeTurns(turnDelta);

        if (immediateEndReason.HasValue)
        {
            loopManager.EndRun(immediateEndReason.Value);
            return true;
        }

        if (turnManager.RemainingTurns <= 0)
        {
            loopManager.EndRun(RunEndReason.EndingOne);
        }

        return true;
    }
}
