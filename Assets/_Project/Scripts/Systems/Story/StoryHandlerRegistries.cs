using System;
using System.Collections.Generic;

public sealed class StoryActionRegistry
{
    private readonly Dictionary<string, IStoryActionHandler> handlers =
        new Dictionary<string, IStoryActionHandler>(StringComparer.Ordinal);

    public StoryActionRegistry()
    {
        Register(new SetSceneObjectActiveStoryAction());
        Register(new SetStoryFlagStoryAction());
        Register(new HideCgStoryAction());
        Register(new AcquireWrenchStoryAction());
        Register(new RemoveRailAndAscendStoryAction());
        Register(new ReleaseFloatingItemAndRiseStoryAction());
        Register(new PlaySfxStoryAction());
        Register(new SwitchBgmStoryAction());
        Register(new WorldPropCommandStoryAction());
        Register(new SpendTurnsStoryAction());
        Register(new ChangeTurnsStoryAction());
        Register(new RequestRunEndStoryAction());
    }

    public void Register(IStoryActionHandler handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        if (string.IsNullOrWhiteSpace(handler.Id))
        {
            throw new ArgumentException("Story action handler id cannot be empty.", nameof(handler));
        }

        if (handlers.ContainsKey(handler.Id))
        {
            throw new InvalidOperationException(
                $"Story action handler '{handler.Id}' is already registered.");
        }

        handlers.Add(handler.Id, handler);
    }

    public bool TryGet(string id, out IStoryActionHandler handler)
    {
        if (string.IsNullOrEmpty(id))
        {
            handler = null;
            return false;
        }

        return handlers.TryGetValue(id, out handler);
    }
}

public sealed class StoryConditionRegistry
{
    private readonly Dictionary<string, IStoryConditionHandler> handlers =
        new Dictionary<string, IStoryConditionHandler>(StringComparer.Ordinal);

    public StoryConditionRegistry()
    {
        Register(new StoryFlagEqualsCondition());
        Register(new PlayerHasWrenchCondition());
        Register(new PlayerRailRemovedCondition());
        Register(new PlayerCanRemoveRailCondition());
        Register(new PlayerHasSlotItemCondition());
        Register(new PlayerIsWorldLayerCondition());
        Register(new WorldPropCommandAvailableCondition());
        Register(new WorldPropCommandUnavailableCondition());
        Register(new RunFlagEqualsCondition());
        Register(new LoopProgressFlagEqualsCondition());
    }

    public void Register(IStoryConditionHandler handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        if (string.IsNullOrWhiteSpace(handler.Id))
        {
            throw new ArgumentException("Story condition handler id cannot be empty.", nameof(handler));
        }

        if (handlers.ContainsKey(handler.Id))
        {
            throw new InvalidOperationException(
                $"Story condition handler '{handler.Id}' is already registered.");
        }

        handlers.Add(handler.Id, handler);
    }

    public bool TryGet(string id, out IStoryConditionHandler handler)
    {
        if (string.IsNullOrEmpty(id))
        {
            handler = null;
            return false;
        }

        return handlers.TryGetValue(id, out handler);
    }
}
