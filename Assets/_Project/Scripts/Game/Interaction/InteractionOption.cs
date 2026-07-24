using System;

public readonly struct InteractionOption
{
    public InteractionOption(
        string label,
        InteractionType type,
        Action execute,
        bool consumesTurn = true)
    {
        Label = label;
        Type = type;
        Execute = execute;
        ConsumesTurn = consumesTurn;
    }

    public string Label { get; }
    public InteractionType Type { get; }
    public Action Execute { get; }
    public bool ConsumesTurn { get; }

    public bool CanExecute => Execute != null;

    public void Invoke()
    {
        Execute?.Invoke();
    }
}
