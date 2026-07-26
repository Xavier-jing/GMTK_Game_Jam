using System;
using UnityEngine;

public sealed class TurnManager
{
    public event Action<int, int> OnTurnsChanged;

    public int RemainingTurns { get; private set; }
    public int InitialTurns { get; private set; }

    public TurnManager(int initialTurns)
    {
        InitialTurns = Mathf.Max(1, initialTurns);
        RemainingTurns = InitialTurns;
    }

    /// <summary>
    /// 增加或减少剩余回合。
    /// 负数表示消耗，正数表示增加。
    /// </summary>
    public void ChangeTurns(int delta)
    {
        long updatedTurns = (long)RemainingTurns + delta;
        RemainingTurns = (int)Math.Max(
            0L,
            Math.Min(int.MaxValue, updatedTurns));
        OnTurnsChanged?.Invoke(RemainingTurns, InitialTurns);
    }

    public void ResetTurns()
    {
        RemainingTurns = InitialTurns;
        OnTurnsChanged?.Invoke(RemainingTurns, InitialTurns);
    }
}