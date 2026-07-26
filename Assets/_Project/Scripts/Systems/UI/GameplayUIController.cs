using UnityEngine;

public sealed class GameplayUIController : MonoBehaviour
{
    private const ScreenId PauseScreenId = ScreenId.Pause;
    private const string CrashSfxId = "IA_Fall_Break";

    private static GameplayUIController activeController;

    [SerializeField]
    private ScreenId gameplayScreen = ScreenId.Hud;

    [SerializeField]
    private UIService uiService;

    [Header("Ending Presentation")]
    [SerializeField]
    private EndingSequencePresenter endingPresenter;

    private GamePause gamePause;
    private LoopManager loopManager;
    private AudioService audioService;
    private UIInputHandler uiInput;
    private bool isEndingPresentationActive;
    private int endingPresentationStartedFrame = -1;
    private bool isActiveController;

    private void Awake()
    {
        if (activeController != null && activeController != this)
        {
            Debug.LogError(
                $"Duplicate GameplayUIController detected on '{name}'. " +
                $"'{activeController.name}' is already the active controller. " +
                "Remove the duplicate component from the GamePlay scene.",
                this);
            enabled = false;
            return;
        }

        activeController = this;
        isActiveController = true;

        if (uiService == null)
        {
            uiService = ResolveUiService();
        }

        uiInput = ResolveUiInput();
        if (endingPresenter == null)
        {
            endingPresenter = GetComponent<EndingSequencePresenter>();
        }
    }

    private void Start()
    {
        if (!isActiveController)
        {
            return;
        }

        AppContext appContext = AppContext.Instance;
        gamePause = appContext.GamePause;
        loopManager = appContext.LoopManager;
        audioService = appContext.Audio;

        if (uiInput != null)
        {
            uiInput.OnPause += HandlePausePerformed;
            uiInput.OnCancel += HandleCancelPerformed;
            uiInput.OnSubmit += HandleSubmitPerformed;
        }

        gamePause.Resume();
        gamePause.PauseStateChanged += HandlePauseStateChanged;
        loopManager.RunEnded += HandleRunEnded;
        HandlePauseStateChanged(gamePause.IsPaused);
    }

    private void OnDestroy()
    {
        if (activeController == this)
        {
            activeController = null;
        }

        if (gamePause != null)
        {
            gamePause.PauseStateChanged -= HandlePauseStateChanged;
        }

        if (loopManager != null)
        {
            loopManager.RunEnded -= HandleRunEnded;
        }

        if (uiInput != null)
        {
            uiInput.OnPause -= HandlePausePerformed;
            uiInput.OnCancel -= HandleCancelPerformed;
            uiInput.OnSubmit -= HandleSubmitPerformed;
        }
    }

    private void HandlePauseStateChanged(bool isPaused)
    {
        if (uiInput != null)
        {
            if (isPaused)
            {
                uiInput.EnableUINavigation();
            }
            else
            {
                uiInput.DisableUINavigation();
            }
        }

        if (uiService == null)
        {
            Debug.LogWarning($"GameplayUIController on '{name}' is missing a UIService reference.");
            return;
        }

        if (isPaused)
        {
            ShowPauseRoot();
        }
        else
        {
            uiService.Show(gameplayScreen);
        }
    }

    private void HandlePausePerformed()
    {
        if (loopManager != null && loopManager.IsEndingRun)
        {
            return;
        }

        OpenPauseScreen();
    }

    private void HandleCancelPerformed()
    {
        if (loopManager != null && loopManager.IsEndingRun)
        {
            return;
        }

        OpenPauseScreen();
    }

    private void HandleRunEnded(RunEndReason reason, int run)
    {
        if (isEndingPresentationActive)
        {
            Debug.LogWarning(
                $"GameplayUIController on '{name}' ignored duplicate ending " +
                $"'{reason}' on run {run} because a presentation is active.",
                this);
            return;
        }

        isEndingPresentationActive = true;
        endingPresentationStartedFrame = Time.frameCount;
        PlayCrashSfx(reason);

        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.SetControlled(false);
        }

        if (gamePause != null && gamePause.IsPaused)
        {
            gamePause.Resume();
        }

        if (endingPresenter == null)
        {
            Debug.LogError(
                $"GameplayUIController on '{name}' has no " +
                $"EndingSequencePresenter. Skipping the presentation for " +
                $"'{reason}' on run {run}.",
                this);
            CompleteEndingCg();
            return;
        }

        if (!endingPresenter.TryPresent(
                reason,
                audioService,
                CompleteEndingCg,
                out string error))
        {
            Debug.LogError(
                $"GameplayUIController on '{name}' could not start ending " +
                $"'{reason}' on run {run}: {error}",
                this);
            CompleteEndingCg();
        }
    }

    private void HandleSubmitPerformed()
    {
        if (loopManager == null ||
            !loopManager.IsEndingRun ||
            !isEndingPresentationActive ||
            Time.frameCount == endingPresentationStartedFrame ||
            endingPresenter == null)
        {
            return;
        }

        endingPresenter.Advance();
    }

    private void PlayCrashSfx(RunEndReason reason)
    {
        if (reason != RunEndReason.EndingOne &&
            reason != RunEndReason.TruthRevealed)
        {
            return;
        }

        if (audioService == null)
        {
            Debug.LogError(
                $"GameplayUIController on '{name}' cannot play '{CrashSfxId}' " +
                "because AudioService is unavailable.");
            return;
        }

        if (!audioService.TryPlaySfxById(CrashSfxId, 1f, out string error))
        {
            Debug.LogError(
                $"GameplayUIController on '{name}' could not play crash SFX " +
                $"'{CrashSfxId}' for ending '{reason}': {error}");
        }
    }

    public void CompleteEndingCg()
    {
        if (loopManager == null || !loopManager.IsEndingRun)
        {
            return;
        }

        isEndingPresentationActive = false;
        endingPresentationStartedFrame = -1;
        loopManager.CompleteRunEnding();
    }

    private UIService ResolveUiService()
    {
        UIService found = GetComponent<UIService>();
        if (found != null)
        {
            return found;
        }

        found = GetComponentInChildren<UIService>(true);
        if (found != null)
        {
            return found;
        }

        found = GetComponentInParent<UIService>(true);
        if (found != null)
        {
            return found;
        }

        return FindObjectOfType<UIService>(true);
    }

    private UIInputHandler ResolveUiInput()
    {
        UIInputHandler found = GetComponent<UIInputHandler>();
        if (found != null)
        {
            return found;
        }

        found = GetComponentInChildren<UIInputHandler>(true);
        if (found != null)
        {
            return found;
        }

        found = GetComponentInParent<UIInputHandler>(true);
        if (found != null)
        {
            return found;
        }

        found = FindObjectOfType<UIInputHandler>(true);
        if (found != null)
        {
            return found;
        }

        return gameObject.AddComponent<UIInputHandler>();
    }

    private void ShowPauseRoot()
    {
        if (uiService.TryGet(PauseScreenId, out _))
        {
            uiService.Show(PauseScreenId);
            return;
        }

        Debug.LogWarning($"GameplayUIController on '{name}' could not find '{PauseScreenId}' screen.");
    }

    private void OpenPauseScreen()
    {
        if (gamePause == null || uiService == null)
        {
            return;
        }

        if (!gamePause.IsPaused)
        {
            gamePause.SetPaused(true);
            return;
        }

        ShowPauseRoot();
    }
}
