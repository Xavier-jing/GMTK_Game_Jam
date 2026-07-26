using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class WorldPropCommandStoryAction : IStoryActionHandler
{
    public string Id => "WorldPropCommand";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        if (!TryValidateWorldPropParameters(
                parameters,
                out WorldPropCommand _,
                out error))
        {
            return false;
        }

        return true;
    }

    public Task<StoryActionResult> ExecuteAsync(
        StoryActionContext context,
        StoryActionParams parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!context.Targets.TryGet(parameters.TargetId, out StoryTarget target))
        {
            return Failure(
                $"Story target '{parameters.TargetId}' was not found in the active scene.");
        }

        WorldStoryInteractable prop = target.GetComponent<WorldStoryInteractable>();
        if (prop == null)
        {
            return Failure(
                $"Story target '{parameters.TargetId}' has no WorldStoryInteractable component.");
        }

        if (!Enum.TryParse(
                parameters.StringValue,
                true,
                out WorldPropCommand command) ||
            !Enum.IsDefined(typeof(WorldPropCommand), command))
        {
            return Failure(
                $"World prop command '{parameters.StringValue}' is invalid.");
        }

        return prop.TryExecuteCommand(context, command, out string reason)
            ? Task.FromResult(StoryActionResult.Success())
            : Failure(reason);
    }

    internal static bool TryValidateWorldPropParameters(
        StoryActionParams parameters,
        out WorldPropCommand command,
        out string error)
    {
        command = default;
        if (parameters == null ||
            !StoryValidator.IsValidId(parameters.TargetId))
        {
            error = "Params.TargetId is required and must be a valid id.";
            return false;
        }

        if (!Enum.TryParse(parameters.StringValue, true, out command) ||
            !Enum.IsDefined(typeof(WorldPropCommand), command))
        {
            error =
                "Params.StringValue must name a supported WorldPropCommand.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static Task<StoryActionResult> Failure(string error)
    {
        return Task.FromResult(StoryActionResult.Failure(error));
    }
}

public sealed class SpendTurnsStoryAction : IStoryActionHandler
{
    public string Id => "SpendTurns";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        if (parameters == null || parameters.IntValue <= 0)
        {
            error = "Params.IntValue must be a positive turn cost.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public Task<StoryActionResult> ExecuteAsync(
        StoryActionContext context,
        StoryActionParams parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.ActionResolver.ResolveStoryTurnCost(
                parameters.IntValue,
                out bool turnsExhausted))
        {
            return Task.FromResult(
                StoryActionResult.Failure(
                    "The turn cost could not be applied because the run is ending."));
        }

        if (turnsExhausted &&
            !context.TryRequestRunEnd(
                RunEndReason.EndingOne,
                null,
                out string error))
        {
            return Task.FromResult(StoryActionResult.Failure(error));
        }

        return Task.FromResult(StoryActionResult.Success());
    }
}

public sealed class ChangeTurnsStoryAction : IStoryActionHandler
{
    public string Id => "ChangeTurns";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        if (parameters == null)
        {
            error = "Params is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public Task<StoryActionResult> ExecuteAsync(
        StoryActionContext context,
        StoryActionParams parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (parameters.IntValue == 0)
        {
            return Task.FromResult(StoryActionResult.Success());
        }

        return Task.FromResult(
            context.TryChangeTurns(parameters.IntValue, out string error)
                ? StoryActionResult.Success()
                : StoryActionResult.Failure(error));
    }
}

public sealed class RequestRunEndStoryAction : IStoryActionHandler
{
    public string Id => "RequestRunEnd";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        if (parameters == null ||
            !Enum.TryParse(parameters.StringValue, true, out RunEndReason reason) ||
            !Enum.IsDefined(typeof(RunEndReason), reason) ||
            (reason != RunEndReason.EndingTwo &&
             reason != RunEndReason.EndingThree))
        {
            error =
                "Params.StringValue must be 'EndingTwo' or 'EndingThree'.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public Task<StoryActionResult> ExecuteAsync(
        StoryActionContext context,
        StoryActionParams parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Enum.TryParse(
                parameters.StringValue,
                true,
                out RunEndReason reason) ||
            !Enum.IsDefined(typeof(RunEndReason), reason))
        {
            return Task.FromResult(
                StoryActionResult.Failure(
                    $"Run end reason '{parameters.StringValue}' is invalid."));
        }

        return Task.FromResult(
            context.TryRequestRunEnd(reason, null, out string error)
                ? StoryActionResult.Success()
                : StoryActionResult.Failure(error));
    }
}

public sealed class WorldPropCommandAvailableCondition : IStoryConditionHandler
{
    public string Id => "WorldPropCommandAvailable";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        return WorldPropCommandStoryAction.TryValidateWorldPropParameters(
            parameters,
            out WorldPropCommand _,
            out error);
    }

    public bool Evaluate(
        StoryActionContext context,
        StoryActionParams parameters)
    {
        if (!context.Targets.TryGet(parameters.TargetId, out StoryTarget target) ||
            !Enum.TryParse(
                parameters.StringValue,
                true,
                out WorldPropCommand command) ||
            !Enum.IsDefined(typeof(WorldPropCommand), command))
        {
            return false;
        }

        WorldStoryInteractable prop = target.GetComponent<WorldStoryInteractable>();
        return prop != null &&
               prop.CanExecuteCommand(context, command, out string _);
    }
}

public sealed class WorldPropCommandUnavailableCondition : IStoryConditionHandler
{
    public string Id => "WorldPropCommandUnavailable";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        return WorldPropCommandStoryAction.TryValidateWorldPropParameters(
            parameters,
            out WorldPropCommand _,
            out error);
    }

    public bool Evaluate(
        StoryActionContext context,
        StoryActionParams parameters)
    {
        if (!Enum.TryParse(
                parameters.StringValue,
                true,
                out WorldPropCommand command) ||
            !Enum.IsDefined(typeof(WorldPropCommand), command))
        {
            return false;
        }

        // This condition drives retry/failure dialogue. Once the one-shot
        // interaction has succeeded, neither its success nor failure choice
        // should remain visible.
        if ((command == WorldPropCommand.RevealBedSwitch &&
             context.RunState.BedLifted) ||
            (command == WorldPropCommand.AcquireBedRope &&
             context.RunState.RopeTaken))
        {
            return false;
        }

        if (!context.Targets.TryGet(parameters.TargetId, out StoryTarget target))
        {
            return false;
        }

        WorldStoryInteractable prop = target.GetComponent<WorldStoryInteractable>();
        return prop != null &&
               !prop.CanExecuteCommand(context, command, out string _);
    }
}

public sealed class RunFlagEqualsCondition : IStoryConditionHandler
{
    public string Id => "RunFlagEquals";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        if (parameters == null ||
            !Enum.TryParse(parameters.StringValue, true, out RunFlagId flag) ||
            !Enum.IsDefined(typeof(RunFlagId), flag))
        {
            error = "Params.StringValue must name a supported RunFlagId.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public bool Evaluate(
        StoryActionContext context,
        StoryActionParams parameters)
    {
        return Enum.TryParse(
                   parameters.StringValue,
                   true,
                   out RunFlagId flag) &&
               Enum.IsDefined(typeof(RunFlagId), flag) &&
               context.RunState.GetFlag(flag) == parameters.BoolValue;
    }
}

public sealed class LoopProgressFlagEqualsCondition : IStoryConditionHandler
{
    public string Id => "LoopProgressFlagEquals";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        if (parameters == null ||
            !Enum.TryParse(
                parameters.StringValue,
                true,
                out LoopProgressFlag flag) ||
            !Enum.IsDefined(typeof(LoopProgressFlag), flag))
        {
            error =
                "Params.StringValue must name a supported LoopProgressFlag.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public bool Evaluate(
        StoryActionContext context,
        StoryActionParams parameters)
    {
        return Enum.TryParse(
                   parameters.StringValue,
                   true,
                   out LoopProgressFlag flag) &&
               Enum.IsDefined(typeof(LoopProgressFlag), flag) &&
               context.LoopProgress.GetFlag(flag) == parameters.BoolValue;
    }
}
