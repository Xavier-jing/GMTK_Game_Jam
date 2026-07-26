using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuScreen : ScreenBase
{
    [SerializeField]
    private Button startButton;

    [SerializeField]
    private Button settingsButton;

    [SerializeField]
    private Button exitButton;

    [SerializeField]
    private SceneId gameplayScene = SceneId.Gameplay;

    private void Awake()
    {
        AutoBindMissingControls();
        BindButtons();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    public void StartGame()
    {
        if (!AppContext.HasInstance)
        {
            Debug.LogWarning($"MainMenuScreen on '{name}' cannot start game because AppContext is missing.");
            return;
        }

        AppContext.Instance.SceneLoader.LoadScene(gameplayScene);
    }

    public void OpenSettings()
    {
        if (Owner == null)
        {
            Debug.LogWarning($"MainMenuScreen on '{name}' cannot open settings because it is not registered.");
            return;
        }

        if (Owner.TryGet(ScreenId.Settings, out ScreenBase screen) &&
            screen is SettingsScreen settingsScreen)
        {
            settingsScreen.OpenFromMainMenu();
            return;
        }

        Owner.Show(ScreenId.Settings);
    }

    public void ContinueGame()
    {
        if (!AppContext.HasInstance)
        {
            Debug.LogWarning($"MainMenuScreen on '{name}' cannot continue game because AppContext is missing.");
            return;
        }

        AppContext.Instance.SceneLoader.LoadScene(gameplayScene);
    }

    private void AutoBindMissingControls()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            if (startButton == null && IsStartButtonName(button.name))
            {
                startButton = button;
            }
            else if (settingsButton == null && IsSettingsButtonName(button.name))
            {
                settingsButton = button;
            }
            else if (exitButton == null && IsExitButtonName(button.name))
            {
                exitButton = button;
            }
        }
    }

    private static bool IsStartButtonName(string buttonName)
    {
        return buttonName == "Start"
            || buttonName == "StartButton"
            || buttonName == "Play"
            || buttonName == "PlayButton";
    }

    private static bool IsSettingsButtonName(string buttonName)
    {
        return buttonName == "Settings"
            || buttonName == "SettingsButton"
            || buttonName == "Music"
            || buttonName == "AudioButton";
    }

    private static bool IsExitButtonName(string buttonName)
    {
        return buttonName == "Continue"
            || buttonName == "ContinueButton"
            || buttonName == "ContinueGame"
            || buttonName == "Exit"
            || buttonName == "ExitButton"
            || buttonName == "Quit"
            || buttonName == "QuitButton"
            || buttonName == "QuitGame";
    }

    private void BindButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
            settingsButton.onClick.AddListener(OpenSettings);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ContinueGame);
            exitButton.onClick.AddListener(ContinueGame);
        }
    }

    private void UnbindButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ContinueGame);
        }
    }
}
