using UnityEngine;

public sealed class GameplayUIController : MonoBehaviour
{
    [SerializeField]
    private ScreenId gameplayScreen = ScreenId.Hud;

    [SerializeField]
    private ScreenId pauseScreen = ScreenId.Pause;

    [SerializeField]
    private UIService uiService;

    private GamePause gamePause;
    private UIInputHandler uiInput;

    private void Awake()
    {
        if (uiService == null)
        {
            uiService = GetComponentInChildren<UIService>(true);
        }

        uiInput = GetComponent<UIInputHandler>();
    }

    private void Start()
    {
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

        uiService.Show(isPaused ? pauseScreen : gameplayScreen);
    }

    private void HandlePausePerformed()
    {
        if (gamePause != null && !gamePause.IsPaused)
        {
            gamePause.SetPaused(true);
        }
    }

    private void HandleCancelPerformed()
    {
        if (gamePause == null || uiService == null)
        {
            return;
        }

        // 未暂停时，Cancel 键（Esc）作为暂停键使用
        if (!gamePause.IsPaused)
        {
            gamePause.SetPaused(true);
            return;
        }

        if (uiService.CurrentScreenId == ScreenId.Settings &&
            uiService.TryGet(ScreenId.Settings, out ScreenBase screen) &&
            screen is SettingsScreen settingsScreen)
        {
            settingsScreen.GoBack();
            return;
        }

        gamePause.Resume();
    }
}
