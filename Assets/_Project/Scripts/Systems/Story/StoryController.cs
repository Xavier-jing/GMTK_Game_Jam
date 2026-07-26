using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StoryController : MonoBehaviour
{
    private const int MaxAutomaticNodesPerAdvance = 100;

    private sealed class RuntimeChoice
    {
        public RuntimeChoice(StoryChoiceData data, bool isInteractable)
        {
            Data = data;
            IsInteractable = isInteractable;
        }

        public StoryChoiceData Data { get; }

        public bool IsInteractable { get; }
    }

    [SerializeField]
    private StoryPresenter presenter;

    [SerializeField]
    private UIInputHandler uiInput;

    [SerializeField]
    private Player player;

    private readonly Dictionary<string, StoryGraph> graphCache =
        new Dictionary<string, StoryGraph>(StringComparer.Ordinal);
    private readonly List<RuntimeChoice> visibleChoices =
        new List<RuntimeChoice>();
    private readonly List<StoryChoiceViewModel> choiceViewModels =
        new List<StoryChoiceViewModel>();

    private StoryLoader loader;
    private StoryValidator validator;
    private StoryActionRegistry actionRegistry;
    private StoryConditionRegistry conditionRegistry;
    private StoryProgress progress;
    private StoryActionContext actionContext;
    private CancellationTokenSource cancellationSource;
    private StoryGraph currentGraph;
    private StoryNodeDefinition currentNode;
    private int sessionSequence;
    private int activeSessionId;
    private string rootScriptId;
    private bool playerWasControlled;
    private bool playerControlCaptured;

    public event Action<StoryNodeInfo> NodeEntered;

    public event Action<StoryCompletion> Completed;

    public event Action<StoryError> Failed;

    public StoryRunnerState State { get; private set; } = StoryRunnerState.Idle;

    public string CurrentScriptId { get; private set; } = string.Empty;

    public string CurrentNodeId { get; private set; } = string.Empty;

    public bool IsRunning =>
        State == StoryRunnerState.Loading ||
        State == StoryRunnerState.ShowingDialogue ||
        State == StoryRunnerState.WaitingForAdvance ||
        State == StoryRunnerState.ExecutingAction ||
        State == StoryRunnerState.WaitingForChoice;

    private void Awake()
    {
        if (presenter == null)
        {
            presenter = GetComponentInChildren<StoryPresenter>(true);
        }

        if (uiInput == null)
        {
            uiInput = GetComponent<UIInputHandler>();
        }

        actionRegistry = new StoryActionRegistry();
        conditionRegistry = new StoryConditionRegistry();
        loader = new StoryLoader();
        validator = new StoryValidator(actionRegistry, conditionRegistry);
        progress = AppContext.Instance.StoryProgress;
    }

    private void OnEnable()
    {
        if (uiInput != null)
        {
            uiInput.OnSubmit += HandleSubmit;
        }
    }

    private void OnDisable()
    {
        if (uiInput != null)
        {
            uiInput.OnSubmit -= HandleSubmit;
        }

        Cancel();
    }

    private void OnDestroy()
    {
        Cancel();
    }

    public bool TryStart(string scriptId, string startNodeId = null)
    {
        if (IsRunning)
        {
            Cancel();
        }

        if (presenter == null || !presenter.IsConfigured)
        {
            ReportStartFailure(
                scriptId,
                "StoryPresenter is missing or has unassigned required references.");
            return false;
        }

        if (uiInput == null)
        {
            ReportStartFailure(
                scriptId,
                "StoryController is missing its UIInputHandler reference.");
            return false;
        }

        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }

        if (player == null)
        {
            ReportStartFailure(
                scriptId,
                "No Player was found when the story started.");
            return false;
        }

        StoryTarget[] sceneTargets = FindObjectsOfType<StoryTarget>(true);
        StoryTargetRegistry targetRegistry = new StoryTargetRegistry(sceneTargets);
        if (targetRegistry.Errors.Count > 0)
        {
            ReportStartFailure(scriptId, string.Join(" ", targetRegistry.Errors));
            return false;
        }

        AppContext appContext = AppContext.Instance;
        actionContext = new StoryActionContext(
            player,
            progress,
            targetRegistry,
            appContext.Inventory,
            appContext.ActionResolver,
            appContext.LoopManager,
            appContext.LoopProgress,
            appContext.RunState,
            appContext.Audio,
            player.CarrySlot);
        rootScriptId = scriptId;
        CurrentScriptId = scriptId ?? string.Empty;
        CurrentNodeId = startNodeId ?? string.Empty;
        currentGraph = null;
        currentNode = null;
        visibleChoices.Clear();
        choiceViewModels.Clear();

        CaptureAndDisablePlayerControl();
        presenter.ResetPortrait();
        presenter.Hide();

        activeSessionId = ++sessionSequence;
        cancellationSource = new CancellationTokenSource();
        State = StoryRunnerState.Loading;

        _ = StartStoryAsync(
            activeSessionId,
            scriptId,
            startNodeId,
            cancellationSource.Token);
        return true;
    }

    public bool TryAdvance()
    {
        if (State != StoryRunnerState.WaitingForAdvance || IsPaused())
        {
            return false;
        }

        if (presenter.TryCompleteDialogue())
        {
            return true;
        }

        int sessionId = activeSessionId;
        if (!IsSessionActive(sessionId) || cancellationSource == null)
        {
            return false;
        }

        State = StoryRunnerState.ExecutingAction;
        _ = CompleteDialogueAsync(
            sessionId,
            currentNode,
            cancellationSource.Token);
        return true;
    }

    public bool TrySelectChoice(int choiceIndex)
    {
        if (State != StoryRunnerState.WaitingForChoice ||
            IsPaused() ||
            choiceIndex < 0 ||
            choiceIndex >= visibleChoices.Count)
        {
            return false;
        }

        RuntimeChoice choice = visibleChoices[choiceIndex];
        if (!choice.IsInteractable)
        {
            return false;
        }

        int sessionId = activeSessionId;
        if (!IsSessionActive(sessionId) || cancellationSource == null)
        {
            return false;
        }

        State = StoryRunnerState.Loading;
        presenter.Hide();
        _ = FollowChoiceAsync(
            sessionId,
            choice.Data,
            cancellationSource.Token);
        return true;
    }

    public void Cancel()
    {
        if (!IsRunning)
        {
            return;
        }

        State = StoryRunnerState.Canceled;
        CleanupSession();
    }

    private async Task StartStoryAsync(
        int sessionId,
        string scriptId,
        string requestedStartNodeId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!TryGetOrLoadGraph(scriptId, out StoryGraph graph, out string error))
            {
                FailStory(sessionId, new StoryError(scriptId, string.Empty, string.Empty, error));
                return;
            }

            string startNodeId = string.IsNullOrEmpty(requestedStartNodeId)
                ? graph.Document.StartNodeId
                : requestedStartNodeId;

            if (!graph.ContainsNode(startNodeId))
            {
                FailStory(
                    sessionId,
                    new StoryError(
                        scriptId,
                        startNodeId,
                        string.Empty,
                        "Requested start node was not found."));
                return;
            }

            await EnterNodeAsync(
                sessionId,
                graph,
                startNodeId,
                automaticNodeCount: 0,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancellation cleanup is owned by Cancel or session replacement.
        }
        catch (Exception exception)
        {
            FailStory(
                sessionId,
                new StoryError(
                    CurrentScriptId,
                    CurrentNodeId,
                    string.Empty,
                    $"Unhandled story exception: {exception.Message}"));
        }
    }

    private async Task EnterNodeAsync(
        int sessionId,
        StoryGraph graph,
        string nodeId,
        int automaticNodeCount,
        CancellationToken cancellationToken)
    {
        while (IsSessionActive(sessionId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!graph.TryGetNode(nodeId, out StoryNodeDefinition node))
            {
                FailStory(
                    sessionId,
                    new StoryError(
                        graph.Document.ScriptId,
                        nodeId,
                        string.Empty,
                        "Node was not found."));
                return;
            }

            currentGraph = graph;
            currentNode = node;
            CurrentScriptId = graph.Document.ScriptId;
            CurrentNodeId = node.Data.Id;
            NodeEntered?.Invoke(
                new StoryNodeInfo(CurrentScriptId, CurrentNodeId, node.Type));

            if (!IsSessionActive(sessionId))
            {
                return;
            }

            switch (node.Type)
            {
                case StoryNodeType.Action:
                    automaticNodeCount++;
                    if (automaticNodeCount > MaxAutomaticNodesPerAdvance)
                    {
                        FailStory(
                            sessionId,
                            new StoryError(
                                CurrentScriptId,
                                CurrentNodeId,
                                string.Empty,
                                $"Exceeded {MaxAutomaticNodesPerAdvance} automatic nodes without player input."));
                        return;
                    }

                    if (!await ExecuteActionsAsync(
                            sessionId,
                            node.Data.Actions,
                            cancellationToken))
                    {
                        return;
                    }

                    nodeId = node.Data.Next;
                    continue;

                case StoryNodeType.Dialogue:
                    if (!await ExecuteActionsAsync(
                            sessionId,
                            node.Data.BeforeActions,
                            cancellationToken))
                    {
                        return;
                    }

                    if (!IsSessionActive(sessionId))
                    {
                        return;
                    }

                    State = StoryRunnerState.ShowingDialogue;
                    presenter.ShowDialogue(
                        node.Data.ActorId,
                        node.Data.PortraitId,
                        ResolveDialogue(node.Data));
                    State = StoryRunnerState.WaitingForAdvance;
                    return;

                case StoryNodeType.Choice:
                    ShowChoices(sessionId, node);
                    return;

                case StoryNodeType.End:
                    CompleteStory(
                        sessionId,
                        new StoryCompletion(
                            rootScriptId,
                            CurrentScriptId,
                            CurrentNodeId,
                            node.Data.Result));
                    return;

                default:
                    FailStory(
                        sessionId,
                        new StoryError(
                            CurrentScriptId,
                            CurrentNodeId,
                            string.Empty,
                            $"Unsupported runtime node type '{node.Type}'."));
                    return;
            }
        }
    }

    private async Task CompleteDialogueAsync(
        int sessionId,
        StoryNodeDefinition dialogueNode,
        CancellationToken cancellationToken)
    {
        try
        {
            if (dialogueNode == null ||
                !await ExecuteActionsAsync(
                    sessionId,
                    dialogueNode.Data.AfterActions,
                    cancellationToken))
            {
                return;
            }

            if (!IsSessionActive(sessionId))
            {
                return;
            }

            presenter.Hide();
            await EnterNodeAsync(
                sessionId,
                currentGraph,
                dialogueNode.Data.Next,
                automaticNodeCount: 0,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancellation cleanup is owned by Cancel or session replacement.
        }
        catch (Exception exception)
        {
            FailStory(
                sessionId,
                new StoryError(
                    CurrentScriptId,
                    CurrentNodeId,
                    string.Empty,
                    $"Failed to advance dialogue: {exception.Message}"));
        }
    }

    private async Task FollowChoiceAsync(
        int sessionId,
        StoryChoiceData choice,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!TryGetOrLoadGraph(
                    choice.TargetScriptId,
                    out StoryGraph targetGraph,
                    out string error))
            {
                FailStory(
                    sessionId,
                    new StoryError(
                        CurrentScriptId,
                        CurrentNodeId,
                        string.Empty,
                        error));
                return;
            }

            if (!targetGraph.ContainsNode(choice.TargetNodeId))
            {
                FailStory(
                    sessionId,
                    new StoryError(
                        choice.TargetScriptId,
                        choice.TargetNodeId,
                        string.Empty,
                        "Choice target node was not found."));
                return;
            }

            await EnterNodeAsync(
                sessionId,
                targetGraph,
                choice.TargetNodeId,
                automaticNodeCount: 0,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancellation cleanup is owned by Cancel or session replacement.
        }
        catch (Exception exception)
        {
            FailStory(
                sessionId,
                new StoryError(
                    CurrentScriptId,
                    CurrentNodeId,
                    string.Empty,
                    $"Failed to follow choice: {exception.Message}"));
        }
    }

    private async Task<bool> ExecuteActionsAsync(
        int sessionId,
        StoryActionData[] actions,
        CancellationToken cancellationToken)
    {
        if (actions == null || actions.Length == 0)
        {
            return true;
        }

        State = StoryRunnerState.ExecutingAction;

        foreach (StoryActionData action in actions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsSessionActive(sessionId))
            {
                return false;
            }

            if (!actionRegistry.TryGet(
                    action.Id,
                    out IStoryActionHandler handler))
            {
                FailStory(
                    sessionId,
                    new StoryError(
                        CurrentScriptId,
                        CurrentNodeId,
                        action.Id,
                        "Action handler is not registered."));
                return false;
            }

            StoryActionResult result;
            try
            {
                result = await handler.ExecuteAsync(
                    actionContext,
                    action.Params,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                FailStory(
                    sessionId,
                    new StoryError(
                        CurrentScriptId,
                        CurrentNodeId,
                        action.Id,
                        $"Action threw an exception: {exception.Message}"));
                return false;
            }

            if (!result.Succeeded)
            {
                FailStory(
                    sessionId,
                    new StoryError(
                        CurrentScriptId,
                        CurrentNodeId,
                        action.Id,
                        result.Error));
                return false;
            }
        }

        return IsSessionActive(sessionId);
    }

    private void ShowChoices(int sessionId, StoryNodeDefinition choiceNode)
    {
        visibleChoices.Clear();
        choiceViewModels.Clear();

        foreach (StoryChoiceData choice in choiceNode.Data.Choices)
        {
            bool conditionMet = true;
            StoryUnavailableMode unavailableMode = StoryUnavailableMode.Disabled;

            if (StoryValidator.HasCondition(choice.Condition))
            {
                if (!conditionRegistry.TryGet(
                        choice.Condition.Id,
                        out IStoryConditionHandler handler))
                {
                    FailStory(
                        sessionId,
                        new StoryError(
                            CurrentScriptId,
                            CurrentNodeId,
                            choice.Condition.Id,
                            "Condition handler is not registered."));
                    return;
                }

                StoryValidator.TryParseUnavailableMode(
                    choice.Condition.UnavailableMode,
                    out unavailableMode);

                try
                {
                    conditionMet = handler.Evaluate(
                        actionContext,
                        choice.Condition.Params);
                }
                catch (Exception exception)
                {
                    FailStory(
                        sessionId,
                        new StoryError(
                            CurrentScriptId,
                            CurrentNodeId,
                            choice.Condition.Id,
                            $"Condition threw an exception: {exception.Message}"));
                    return;
                }
            }

            if (!conditionMet && unavailableMode == StoryUnavailableMode.Hidden)
            {
                continue;
            }

            visibleChoices.Add(new RuntimeChoice(choice, conditionMet));
            choiceViewModels.Add(
                new StoryChoiceViewModel(choice.Dialog, conditionMet));
        }

        if (visibleChoices.Count == 0)
        {
            FailStory(
                sessionId,
                new StoryError(
                    CurrentScriptId,
                    CurrentNodeId,
                    string.Empty,
                    "Choice node has no visible choices."));
            return;
        }

        presenter.ShowChoices(
            choiceViewModels,
            choiceIndex => TrySelectChoice(choiceIndex));
        State = StoryRunnerState.WaitingForChoice;
    }

    private bool TryGetOrLoadGraph(
        string scriptId,
        out StoryGraph graph,
        out string error)
    {
        if (graphCache.TryGetValue(scriptId, out graph))
        {
            error = string.Empty;
            return true;
        }

        StoryLoadResult loadResult = loader.Load(scriptId);
        if (!loadResult.Succeeded)
        {
            error = loadResult.Error;
            graph = null;
            return false;
        }

        if (!string.Equals(
                loadResult.Document.ScriptId,
                scriptId,
                StringComparison.Ordinal))
        {
            error =
                $"Loaded resource '{scriptId}' declares ScriptId " +
                $"'{loadResult.Document.ScriptId}'.";
            graph = null;
            return false;
        }

        if (!validator.TryValidate(
                loadResult.Document,
                out graph,
                out List<string> errors))
        {
            error = string.Join(" ", errors);
            return false;
        }

        graphCache.Add(scriptId, graph);
        error = string.Empty;
        return true;
    }

    private void CompleteStory(int sessionId, StoryCompletion completion)
    {
        if (!IsSessionActive(sessionId))
        {
            return;
        }

        progress.MarkScriptCompleted(completion.RootScriptId);
        if (!string.Equals(
                completion.RootScriptId,
                completion.ScriptId,
                StringComparison.Ordinal))
        {
            progress.MarkScriptCompleted(completion.ScriptId);
        }

        StoryActionContext completedContext = actionContext;
        string completedScriptId = CurrentScriptId;
        string completedNodeId = CurrentNodeId;
        State = StoryRunnerState.Completed;
        CleanupSession();

        if (completedContext != null &&
            !completedContext.TryCommitCompletion(out string commitError))
        {
            State = StoryRunnerState.Faulted;
            StoryError error = new StoryError(
                completedScriptId,
                completedNodeId,
                "StoryCompletion",
                commitError);
            Debug.LogError($"Story failed: {error}", this);
            Failed?.Invoke(error);
            return;
        }

        Completed?.Invoke(completion);
    }

    private void FailStory(int sessionId, StoryError error)
    {
        if (!IsSessionActive(sessionId))
        {
            return;
        }

        State = StoryRunnerState.Faulted;
        Debug.LogError($"Story failed: {error}", this);
        CleanupSession();
        Failed?.Invoke(error);
    }

    private void ReportStartFailure(string scriptId, string message)
    {
        CurrentScriptId = scriptId ?? string.Empty;
        CurrentNodeId = string.Empty;
        State = StoryRunnerState.Faulted;

        StoryError error = new StoryError(
            CurrentScriptId,
            string.Empty,
            string.Empty,
            message);
        Debug.LogError($"Story failed: {error}", this);
        Failed?.Invoke(error);
    }

    private void CaptureAndDisablePlayerControl()
    {
        playerWasControlled = player.IsControlled;
        playerControlCaptured = true;
        player.SetControlled(false);
    }

    private void CleanupSession()
    {
        activeSessionId = 0;

        if (cancellationSource != null)
        {
            cancellationSource.Cancel();
            cancellationSource.Dispose();
            cancellationSource = null;
        }

        presenter?.Hide();
        visibleChoices.Clear();
        choiceViewModels.Clear();
        actionContext = null;
        currentGraph = null;
        currentNode = null;

        if (playerControlCaptured && player != null)
        {
            player.SetControlled(playerWasControlled);
        }

        playerControlCaptured = false;
    }

    private bool IsSessionActive(int sessionId)
    {
        return sessionId != 0 && sessionId == activeSessionId && IsRunning;
    }

    private bool IsPaused()
    {
        return AppContext.HasInstance && AppContext.Instance.GamePause.IsPaused;
    }

    public static string ResolveDialogue(StoryNodeData node)
    {
        if (node != null &&
            node.DialogOptions != null &&
            node.DialogOptions.Length > 0)
        {
            int index = UnityEngine.Random.Range(0, node.DialogOptions.Length);
            return node.DialogOptions[index];
        }

        return node?.Dialog ?? string.Empty;
    }

    private void HandleSubmit()
    {
        TryAdvance();
    }
}
