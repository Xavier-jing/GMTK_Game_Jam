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
    private InputReader input;

    private void Awake()
    {
        if (uiService == null)
        {
            uiService = GetComponentInChildren<UIService>(true);
        }
    }

    private void Start()
    {
        AppContext appContext = AppContext.Instance;
        gamePause = appContext.GamePause;
        input = appContext.Input;

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
    }

    private void Update()
    {
        if (input.PausePressedThisFrame)
        {
            gamePause.Toggle();
        }
    }

    private void HandlePauseStateChanged(bool isPaused)
    {
        if (uiService == null)
        {
            Debug.LogWarning($"GameplayUIController on '{name}' is missing a UIService reference.");
            return;
        }

        uiService.Show(isPaused ? pauseScreen : gameplayScreen);
    }
}
