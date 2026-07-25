using UnityEngine;
using UnityEngine.UI;

public sealed class PauseScreen : ScreenBase
{
    [SerializeField]
    private Button resumeButton;

    [SerializeField]
    private Button settingsButton;

    [SerializeField]
    private Button mainMenuButton;

    [SerializeField]
    private Button exitButton;

    private void Awake()
    {
        AutoBindMissingControls();
        BindButtons();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    public void ResumeGame()
    {
        if (AppContext.HasInstance && AppContext.Instance.GamePause != null)
        {
            AppContext.Instance.GamePause.Resume();
        }
    }

    public void OpenSettings()
    {
        if (Owner == null)
        {
            Debug.LogWarning($"PauseScreen on '{name}' cannot open settings because it is not registered.");
            return;
        }

        if (Owner.TryGet(ScreenId.Settings, out ScreenBase screen) &&
            screen is SettingsScreen settingsScreen)
        {
            settingsScreen.OpenFromPause();
            return;
        }

        Owner.Show(ScreenId.Settings);
    }

    public void ReturnToMainMenu()
    {
        if (!AppContext.HasInstance)
        {
            return;
        }

        if (AppContext.Instance.GamePause != null)
        {
            AppContext.Instance.GamePause.Resume();
        }

        AppContext.Instance.SceneLoader.LoadScene(SceneId.MainMenu);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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

            if (resumeButton == null && IsResumeButtonName(button.name))
            {
                resumeButton = button;
            }
            else if (settingsButton == null && IsSettingsButtonName(button.name))
            {
                settingsButton = button;
            }
            else if (mainMenuButton == null && IsMainMenuButtonName(button.name))
            {
                mainMenuButton = button;
            }
            else if (exitButton == null && IsExitButtonName(button.name))
            {
                exitButton = button;
            }
        }
    }

    private static bool IsResumeButtonName(string buttonName)
    {
        return buttonName == "Resume"
            || buttonName == "ResumeButton"
            || buttonName == "ContinueButton";
    }

    private static bool IsSettingsButtonName(string buttonName)
    {
        return buttonName == "Settings"
            || buttonName == "SettingsButton"
            || buttonName == "Music"
            || buttonName == "AudioButton";
    }

    private static bool IsMainMenuButtonName(string buttonName)
    {
        return buttonName == "MainMenu"
            || buttonName == "MainMenuButton"
            || buttonName == "BackToMainMenuButton"
            || buttonName == "BackToMenuButton";
    }

    private static bool IsExitButtonName(string buttonName)
    {
        return buttonName == "Exit"
            || buttonName == "ExitButton"
            || buttonName == "Quit"
            || buttonName == "QuitButton"
            || buttonName == "QuitGame";
    }

    private void BindButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
            settingsButton.onClick.AddListener(OpenSettings);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitGame);
            exitButton.onClick.AddListener(ExitGame);
        }
    }

    private void UnbindButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitGame);
        }
    }
}
