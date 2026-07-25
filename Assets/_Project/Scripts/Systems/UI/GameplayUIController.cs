using UnityEngine;

public sealed class GameplayUIController : MonoBehaviour
{
    private const ScreenId PauseScreenId = ScreenId.Pause;

    private static GameplayUIController activeController;

    [SerializeField]
    private ScreenId gameplayScreen = ScreenId.Hud;

    [SerializeField]
    private UIService uiService;

    private GamePause gamePause;
    private UIInputHandler uiInput;
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
    }

    private void Start()
    {
        if (!isActiveController)
        {
            return;
        }

        AppContext appContext = AppContext.Instance;
        gamePause = appContext.GamePause;

        if (uiInput != null)
        {
            uiInput.OnPause += HandlePausePerformed;
            uiInput.OnCancel += HandleCancelPerformed;
        }

        gamePause.Resume();
        gamePause.PauseStateChanged += HandlePauseStateChanged;
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
        OpenPauseScreen();
    }

    private void HandleCancelPerformed()
    {
        OpenPauseScreen();
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
