using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

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

    void Show();

    void ShowDialogue(string actorId, string portraitId, string dialog);

    void ShowCg(Sprite cgSprite);

    void HideCg();

    void ShowChoices(
        IReadOnlyList<StoryChoiceViewModel> choices,
        Action<int> onSelected);

    void LockChoices();

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
    private readonly Action hideCgAction;
    private Action completionAction;
    private RunEndReason? pendingRunEndReason;

    public StoryActionContext(
        Player player,
        StoryProgress progress,
        StoryTargetRegistry targets,
        Inventory inventory,
        ActionResolver actionResolver,
        LoopManager loopManager,
        LoopProgress loopProgress,
        RunState runState,
        AudioService audio,
        PlayerCarrySlot carrySlot,
        Action hideCgAction)
    {
        Player = player;
        Progress = progress ?? throw new ArgumentNullException(nameof(progress));
        Targets = targets ?? throw new ArgumentNullException(nameof(targets));
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        ActionResolver = actionResolver ?? throw new ArgumentNullException(nameof(actionResolver));
        LoopManager = loopManager ?? throw new ArgumentNullException(nameof(loopManager));
        LoopProgress = loopProgress ?? throw new ArgumentNullException(nameof(loopProgress));
        RunState = runState ?? throw new ArgumentNullException(nameof(runState));
        Audio = audio ?? throw new ArgumentNullException(nameof(audio));
        CarrySlot = carrySlot;
        this.hideCgAction =
            hideCgAction ?? throw new ArgumentNullException(nameof(hideCgAction));
    }

    public Player Player { get; }

    public StoryProgress Progress { get; }

    public StoryTargetRegistry Targets { get; }

    public Inventory Inventory { get; }

    public ActionResolver ActionResolver { get; }

    public LoopManager LoopManager { get; }

    public LoopProgress LoopProgress { get; }

    public RunState RunState { get; }

    public AudioService Audio { get; }

    public PlayerCarrySlot CarrySlot { get; }

    public void HideCg()
    {
        hideCgAction.Invoke();
    }

    public bool TryChangeTurns(int turnDelta, out string error)
    {
        if (!ActionResolver.ResolveStoryTurnDelta(
                turnDelta,
                out bool turnsExhausted))
        {
            error =
                "The turn change could not be applied because it was zero or the run is ending.";
            return false;
        }

        if (turnsExhausted &&
            !TryRequestRunEnd(
                RunEndReason.EndingOne,
                null,
                out error))
        {
            return false;
        }

        error = string.Empty;
        return true;
    }

    public bool TryRequestRunEnd(
        RunEndReason reason,
        Action onStoryCompleted,
        out string error)
    {
        if (pendingRunEndReason.HasValue)
        {
            error =
                $"A run end is already pending with reason '{pendingRunEndReason.Value}'.";
            return false;
        }

        if (!LoopManager.CanEndRun(reason))
        {
            error = $"Run end reason '{reason}' is not currently allowed.";
            return false;
        }

        pendingRunEndReason = reason;
        completionAction = onStoryCompleted;
        error = string.Empty;
        return true;
    }

    public bool TryCommitCompletion(out string error)
    {
        RunEndReason? endReason = pendingRunEndReason;
        Action action = completionAction;
        pendingRunEndReason = null;
        completionAction = null;

        if (endReason.HasValue && !LoopManager.CanEndRun(endReason.Value))
        {
            error =
                $"Run end reason '{endReason.Value}' became unavailable before story completion.";
            return false;
        }

        try
        {
            action?.Invoke();
        }
        catch (Exception exception)
        {
            error = $"Deferred story completion failed: {exception.Message}";
            return false;
        }

        if (!endReason.HasValue)
        {
            error = string.Empty;
            return true;
        }

        LoopManager.EndRun(endReason.Value);
        error = string.Empty;
        return true;
    }
}
