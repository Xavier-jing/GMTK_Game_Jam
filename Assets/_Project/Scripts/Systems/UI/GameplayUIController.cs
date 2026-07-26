using System.Collections;
using UnityEngine;

public sealed class GameplayUIController : MonoBehaviour
{
    private const ScreenId PauseScreenId = ScreenId.Pause;

    private static GameplayUIController activeController;

    [SerializeField]
    private ScreenId gameplayScreen = ScreenId.Hud;

    [SerializeField]
    private UIService uiService;

    [Header("Ending CG")]
    [SerializeField]
    private GameObject endingCgRoot;

    [SerializeField]
    [Min(0f)]
    private float endingCgPlaceholderDuration = 2f;

    [SerializeField]
    private bool autoCompleteEndingCg = true;

    private GamePause gamePause;
    private LoopManager loopManager;
    private UIInputHandler uiInput;
    private Coroutine endingCgCoroutine;
    private bool isActiveController;

    private void Awake()
    {
        if (activeController != null && activeController != this)
        {
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

        if (endingCgRoot != null)
        {
            endingCgRoot.SetActive(false);
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

        if (uiInput != null)
        {
            uiInput.OnPause += HandlePausePerformed;
            uiInput.OnCancel += HandleCancelPerformed;
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
        if (endingCgCoroutine != null)
        {
            return;
        }

        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.SetControlled(false);
        }

        if (uiInput != null)
        {
            uiInput.Disable();
        }

        if (gamePause != null && gamePause.IsPaused)
        {
            gamePause.Resume();
        }

        if (endingCgRoot == null)
        {
            Debug.LogWarning(
                $"GameplayUIController on '{name}' has no Ending CG Root. " +
                $"Skipping the placeholder for '{reason}' on run {run}.");
            CompleteEndingCg();
            return;
        }

        endingCgRoot.SetActive(true);
        if (autoCompleteEndingCg)
        {
            endingCgCoroutine = StartCoroutine(WaitForEndingCgPlaceholder());
        }
    }

    private IEnumerator WaitForEndingCgPlaceholder()
    {
        if (endingCgPlaceholderDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(endingCgPlaceholderDuration);
        }
        else
        {
            yield return null;
        }

        endingCgCoroutine = null;
        CompleteEndingCg();
    }

    public void CompleteEndingCg()
    {
        if (loopManager == null || !loopManager.IsEndingRun)
        {
            return;
        }

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
