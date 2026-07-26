using System;

public sealed class LoopProgress
{
    public event Action Changed;

    public int CurrentLoop { get; private set; } = 1;

    public bool TruthKnown { get; private set; }

    public bool EndingTwoReached { get; private set; }

    public bool EndingThreeReached { get; private set; }

    public void StartNextLoop()
    {
        CurrentLoop++;
        Changed?.Invoke();
    }

    public void RevealTruth()
    {
        if (TruthKnown)
        {
            return;
        }

        TruthKnown = true;
        Changed?.Invoke();
    }

    public void MarkEndingTwoReached()
    {
        if (EndingTwoReached)
        {
            return;
        }

        EndingTwoReached = true;
        Changed?.Invoke();
    }

    public void MarkEndingThreeReached()
    {
        if (EndingThreeReached)
        {
            return;
        }

        EndingThreeReached = true;
        Changed?.Invoke();
    }

    public bool GetFlag(LoopProgressFlag flag)
    {
        switch (flag)
        {
            case LoopProgressFlag.TruthKnown:
                return TruthKnown;
            case LoopProgressFlag.EndingTwoReached:
                return EndingTwoReached;
            case LoopProgressFlag.EndingThreeReached:
                return EndingThreeReached;
            default:
                return false;
        }
    }
}
