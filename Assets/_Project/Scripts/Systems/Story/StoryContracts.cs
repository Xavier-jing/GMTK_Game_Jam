using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class StoryNodeInfo
{
    public StoryNodeInfo(string scriptId, string nodeId, StoryNodeType nodeType)
    {
        ScriptId = scriptId;
        NodeId = nodeId;
        NodeType = nodeType;
    }

    public string ScriptId { get; }

    public string NodeId { get; }

    public StoryNodeType NodeType { get; }
}

public sealed class StoryCompletion
{
    public StoryCompletion(
        string rootScriptId,
        string scriptId,
        string nodeId,
        string result)
    {
        RootScriptId = rootScriptId;
        ScriptId = scriptId;
        NodeId = nodeId;
        Result = result ?? string.Empty;
    }

    public string RootScriptId { get; }

    public string ScriptId { get; }

    public string NodeId { get; }

    public string Result { get; }
}

public sealed class StoryError
{
    public StoryError(
        string scriptId,
        string nodeId,
        string handlerId,
        string message)
    {
        ScriptId = scriptId ?? string.Empty;
        NodeId = nodeId ?? string.Empty;
        HandlerId = handlerId ?? string.Empty;
        Message = message ?? string.Empty;
    }

    public string ScriptId { get; }

    public string NodeId { get; }

    public string HandlerId { get; }

    public string Message { get; }

    public override string ToString()
    {
        string location = string.IsNullOrEmpty(NodeId)
            ? ScriptId
            : $"{ScriptId}/{NodeId}";
        string handler = string.IsNullOrEmpty(HandlerId)
            ? string.Empty
            : $"/{HandlerId}";
        return $"{location}{handler}: {Message}";
    }
}

public readonly struct StoryActionResult
{
    private StoryActionResult(bool succeeded, string error)
    {
        Succeeded = succeeded;
        Error = error ?? string.Empty;
    }

    public bool Succeeded { get; }

    public string Error { get; }

    public static StoryActionResult Success()
    {
        return new StoryActionResult(true, string.Empty);
    }

    public static StoryActionResult Failure(string error)
    {
        return new StoryActionResult(false, error);
    }
}

public sealed class StoryChoiceViewModel
{
    public StoryChoiceViewModel(string dialog, bool isInteractable)
    {
        Dialog = dialog ?? string.Empty;
        IsInteractable = isInteractable;
    }

    public string Dialog { get; }

    public bool IsInteractable { get; }
}

public interface IStoryPresenter
{
    bool IsConfigured { get; }

    void ShowDialogue(string actorId, string dialog);

    void ShowChoices(
        IReadOnlyList<StoryChoiceViewModel> choices,
        Action<int> onSelected);

    bool TryCompleteDialogue();

    void Hide();
}

public interface IStoryActionHandler
{
    string Id { get; }

    bool Validate(StoryActionParams parameters, out string error);

    Task<StoryActionResult> ExecuteAsync(
        StoryActionContext context,
        StoryActionParams parameters,
        CancellationToken cancellationToken);
}

public interface IStoryConditionHandler
{
    string Id { get; }

    bool Validate(StoryActionParams parameters, out string error);

    bool Evaluate(StoryActionContext context, StoryActionParams parameters);
}

public sealed class StoryActionContext
{
    public StoryActionContext(
        Player player,
        StoryProgress progress,
        StoryTargetRegistry targets)
    {
        Player = player;
        Progress = progress ?? throw new ArgumentNullException(nameof(progress));
        Targets = targets ?? throw new ArgumentNullException(nameof(targets));
    }

    public Player Player { get; }

    public StoryProgress Progress { get; }

    public StoryTargetRegistry Targets { get; }
}
