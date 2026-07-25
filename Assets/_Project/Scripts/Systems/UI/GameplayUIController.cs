using UnityEngine;

public sealed class GameplayUIController : MonoBehaviour
{
    [SerializeField]
    private ScreenId gameplayScreen = ScreenId.Hud;

    [SerializeField]
    private ScreenId pauseScreen = ScreenId.Pause;

    [SerializeField]
    private UIService uiService;

    [SerializeField]
    private PlayerInteractor playerInteractor;

    [SerializeField]
    [TextArea]
    private string interactionPromptPrefix = "Press Enter / Space / A: ";

    private GamePause gamePause;
    private HudScreen hudScreen;
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

        BindInteractionPrompt();
    }

    private void OnDestroy()
    {
        if (gamePause != null)
        {
            gamePause.PauseStateChanged -= HandlePauseStateChanged;
        }

        if (playerInteractor != null)
        {
            playerInteractor.PromptChanged -= HandleInteractionPromptChanged;
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

    private void BindInteractionPrompt()
    {
        if (uiService == null ||
            !uiService.TryGet(gameplayScreen, out ScreenBase screen) ||
            !(screen is HudScreen resolvedHudScreen))
        {
            Debug.LogWarning(
                $"GameplayUIController on '{name}' could not resolve a HudScreen for '{gameplayScreen}'.",
                this);
            return;
        }

        hudScreen = resolvedHudScreen;

        if (playerInteractor == null)
        {
            Debug.LogWarning(
                $"GameplayUIController on '{name}' is missing its PlayerInteractor reference.",
                this);
            hudScreen.ClearHint();
            return;
        }

        playerInteractor.PromptChanged += HandleInteractionPromptChanged;
        HandleInteractionPromptChanged(
            playerInteractor.CurrentPrompt,
            playerInteractor.CurrentTargetCanInteract);
    }

    private void HandleInteractionPromptChanged(string prompt, bool canInteract)
    {
        if (hudScreen == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            hudScreen.ClearHint();
            return;
        }

        hudScreen.SetHint(
            canInteract
                ? string.Concat(interactionPromptPrefix, prompt)
                : prompt);
    }
}
