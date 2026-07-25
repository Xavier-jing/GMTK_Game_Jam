public sealed class LoopProgress
{
    public int CurrentLoop { get; private set; } = 1;

    public bool TruthKnown { get; private set; }

    public bool EndingTwoReached { get; private set; }

    public bool EndingThreeReached { get; private set; }

    public void StartNextLoop()
    {
        CurrentLoop++;
    }

    public void RevealTruth()
    {
        TruthKnown = true;
    }

    public void MarkEndingTwoReached()
    {
        EndingTwoReached = true;
    }

    public void MarkEndingThreeReached()
    {
        EndingThreeReached = true;
    }
}
