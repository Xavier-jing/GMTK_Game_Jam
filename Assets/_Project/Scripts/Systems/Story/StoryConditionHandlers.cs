using System;

public sealed class StoryFlagEqualsCondition : IStoryConditionHandler
{
    public string Id => "StoryFlagEquals";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        if (parameters == null || !StoryValidator.IsValidId(parameters.Key))
        {
            error = "Params.Key is required and must be a valid id.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public bool Evaluate(StoryActionContext context, StoryActionParams parameters)
    {
        return context.Progress.GetFlag(parameters.Key) == parameters.BoolValue;
    }
}

public sealed class PlayerHasWrenchCondition : IStoryConditionHandler
{
    public string Id => "PlayerHasWrench";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        error = string.Empty;
        return true;
    }

    public bool Evaluate(StoryActionContext context, StoryActionParams parameters)
    {
        return context.Player != null &&
               context.Player.GameplayStatus.HasWrench == GetExpected(parameters);
    }

    private static bool GetExpected(StoryActionParams parameters)
    {
        return parameters == null || parameters.BoolValue;
    }
}

public sealed class PlayerRailRemovedCondition : IStoryConditionHandler
{
    public string Id => "PlayerRailRemoved";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        error = string.Empty;
        return true;
    }

    public bool Evaluate(StoryActionContext context, StoryActionParams parameters)
    {
        return context.Player != null &&
               context.Player.GameplayStatus.RailRemoved == GetExpected(parameters);
    }

    private static bool GetExpected(StoryActionParams parameters)
    {
        return parameters == null || parameters.BoolValue;
    }
}

public sealed class PlayerCanRemoveRailCondition : IStoryConditionHandler
{
    public string Id => "PlayerCanRemoveRail";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        error = string.Empty;
        return true;
    }

    public bool Evaluate(StoryActionContext context, StoryActionParams parameters)
    {
        return context.Player != null &&
               context.Player.GameplayStatus.HasWrench &&
               !context.Player.GameplayStatus.RailRemoved &&
               !context.Player.GameplayStatus.HasSlotItem;
    }
}

public sealed class PlayerHasSlotItemCondition : IStoryConditionHandler
{
    public string Id => "PlayerHasSlotItem";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        error = string.Empty;
        return true;
    }

    public bool Evaluate(StoryActionContext context, StoryActionParams parameters)
    {
        return context.Player != null &&
               context.Player.GameplayStatus.HasSlotItem == GetExpected(parameters);
    }

    private static bool GetExpected(StoryActionParams parameters)
    {
        return parameters == null || parameters.BoolValue;
    }
}

public sealed class PlayerIsWorldLayerCondition : IStoryConditionHandler
{
    public string Id => "PlayerIsWorldLayer";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        if (parameters == null ||
            !Enum.TryParse(parameters.StringValue, true, out PlayerWorldLayer _))
        {
            error = "Params.StringValue must be 'Lower' or 'Upper'.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public bool Evaluate(StoryActionContext context, StoryActionParams parameters)
    {
        return context.Player != null &&
               Enum.TryParse(
                   parameters.StringValue,
                   true,
                   out PlayerWorldLayer expectedLayer) &&
               context.Player.GameplayStatus.CurrentLayer == expectedLayer;
    }
}
