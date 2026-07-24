using System;

public sealed class TurnManager
{
    public event Action<int, int> OnTurnsChanged;
    public event Action OnTurnsExhausted;

    public int RemainingTurns { get; private set; }
    public int MaxTurns { get; private set; }

    public TurnManager(int maxTurns)
    {
        MaxTurns = maxTurns;
        RemainingTurns = maxTurns;
    }

    public bool CanAct(int cost)
    {
        if (cost < 0)
        {
            return false;
        }

        return RemainingTurns >= cost;
    }

    public bool ConsumeTurn(int cost)
    {
        if (cost < 0)
        {
            return false;
        }

        if (!CanAct(cost))
        {
            return false;
        }

        RemainingTurns -= cost;
        OnTurnsChanged?.Invoke(RemainingTurns, MaxTurns);

        if (RemainingTurns <= 0)
        {
            RemainingTurns = 0;
            OnTurnsExhausted?.Invoke();
        }

        return true;
    }

    public void AddTurns(int amount)
    {
        if (amount < 0)
        {
            return;
        }

        RemainingTurns += amount;
        OnTurnsChanged?.Invoke(RemainingTurns, MaxTurns);
    }
}
