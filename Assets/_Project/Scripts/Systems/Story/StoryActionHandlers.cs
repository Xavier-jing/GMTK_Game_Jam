using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public sealed class SetSceneObjectActiveStoryAction : IStoryActionHandler
{
    public string Id => "SetSceneObjectActive";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        if (parameters == null || !StoryValidator.IsValidId(parameters.TargetId))
        {
            error = "Params.TargetId is required and must be a valid id.";
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

        if (!context.Targets.TryGet(parameters.TargetId, out StoryTarget target))
        {
            return Task.FromResult(
                StoryActionResult.Failure(
                    $"Story target '{parameters.TargetId}' was not found in the active scene."));
        }

        target.gameObject.SetActive(parameters.BoolValue);
        return Task.FromResult(StoryActionResult.Success());
    }
}

public sealed class SetStoryFlagStoryAction : IStoryActionHandler
{
    public string Id => "SetStoryFlag";

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

    public Task<StoryActionResult> ExecuteAsync(
        StoryActionContext context,
        StoryActionParams parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.Progress.SetFlag(parameters.Key, parameters.BoolValue);
        return Task.FromResult(StoryActionResult.Success());
    }
}

public sealed class AcquireWrenchStoryAction : IStoryActionHandler
{
    public string Id => "AcquireWrench";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        error = string.Empty;
        return true;
    }

    public Task<StoryActionResult> ExecuteAsync(
        StoryActionContext context,
        StoryActionParams parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Player == null)
        {
            return Task.FromResult(
                StoryActionResult.Failure("The active story has no Player reference."));
        }

        context.Player.GameplayStatus.AcquireWrench();
        return Task.FromResult(StoryActionResult.Success());
    }
}

public sealed class RemoveRailAndAscendStoryAction : IStoryActionHandler
{
    public string Id => "RemoveRailAndAscend";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        error = string.Empty;
        return true;
    }

    public Task<StoryActionResult> ExecuteAsync(
        StoryActionContext context,
        StoryActionParams parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Player == null)
        {
            return Task.FromResult(
                StoryActionResult.Failure("The active story has no Player reference."));
        }

        bool started = context.Player.TryStartRailRemovedAscend();
        return Task.FromResult(
            started
                ? StoryActionResult.Success()
                : StoryActionResult.Failure(
                    "Player prerequisites for rail removal and ascent were not met."));
    }
}

public sealed class ReleaseFloatingItemAndRiseStoryAction : IStoryActionHandler
{
    public string Id => "ReleaseFloatingItemAndRise";

    public bool Validate(StoryActionParams parameters, out string error)
    {
        error = string.Empty;
        return true;
    }

    public Task<StoryActionResult> ExecuteAsync(
        StoryActionContext context,
        StoryActionParams parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Player == null)
        {
            return Task.FromResult(
                StoryActionResult.Failure("The active story has no Player reference."));
        }

        bool started = context.Player.TryReleaseFloatingItemAndRise();
        return Task.FromResult(
            started
                ? StoryActionResult.Success()
                : StoryActionResult.Failure(
                    "Player prerequisites for releasing the floating item were not met."));
    }
}

public sealed class PlaySfxStoryAction : IStoryActionHandler
{
    public const string ActionId = "PlaySfx";
    private const float DefaultVolume = 1f;

    public string Id => ActionId;

    public bool Validate(StoryActionParams parameters, out string error)
    {
        if (parameters == null ||
            !AudioService.IsValidAudioId(parameters.StringValue))
        {
            error =
                "Params.StringValue is required and must be a valid audio id.";
            return false;
        }

        if (float.IsNaN(parameters.FloatValue) ||
            float.IsInfinity(parameters.FloatValue) ||
            parameters.FloatValue < 0f ||
            parameters.FloatValue > 1f)
        {
            error =
                "Params.FloatValue must be 0 for the default volume or a value from 0 to 1.";
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

        float volume = parameters.FloatValue > 0f
            ? parameters.FloatValue
            : DefaultVolume;
        if (!context.Audio.TryPlaySfxById(
                parameters.StringValue,
                volume,
                out string error))
        {
            Debug.LogError(
                $"[StoryAudio/{ActionId}] Audio id '{parameters.StringValue}' " +
                $"could not be played: {error}");
        }

        return Task.FromResult(StoryActionResult.Success());
    }
}

public sealed class SwitchBgmStoryAction : IStoryActionHandler
{
    public const string ActionId = "SwitchBgm";
    private const float DefaultFadeDuration = 1f;

    public string Id => ActionId;

    public bool Validate(StoryActionParams parameters, out string error)
    {
        if (parameters == null ||
            !AudioService.IsValidAudioId(parameters.StringValue))
        {
            error =
                "Params.StringValue is required and must be a valid audio id.";
            return false;
        }

        if (float.IsNaN(parameters.FloatValue) ||
            float.IsInfinity(parameters.FloatValue) ||
            parameters.FloatValue < 0f)
        {
            error =
                "Params.FloatValue must be 0 for the default fade or a positive duration.";
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

        float fadeDuration = parameters.FloatValue > 0f
            ? parameters.FloatValue
            : DefaultFadeDuration;
        if (!context.Audio.TrySwitchBgmById(
                parameters.StringValue,
                fadeDuration,
                out string error))
        {
            Debug.LogError(
                $"[StoryAudio/{ActionId}] Audio id '{parameters.StringValue}' " +
                $"could not be played: {error}");
        }

        return Task.FromResult(StoryActionResult.Success());
    }
}
