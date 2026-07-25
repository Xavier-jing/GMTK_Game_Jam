using UnityEngine;
using UnityEngine.UI;

public sealed class PauseScreen : ScreenBase
{
    [SerializeField]
    private Button settingsButton;

    [SerializeField]
    private Button mainMenuButton;

    [SerializeField]
    private Button resumeButton;

    private bool buttonsBound;

    private void Awake()
    {
        BindButtons();
    }

    private void OnDestroy()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
        }
    }

    private void BindButtons()
    {
        if (buttonsBound)
        {
            return;
        }

        if (resumeButton == null && mainMenuButton == null && settingsButton == null)
        {
            return;
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OpenSettings);
        }

        buttonsBound = true;
    }

    private static void OpenSettings()
    {
        UIService.Current?.Get<SettingsScreen>(ScreenId.Settings).OpenFrom(ScreenId.Pause);
    }

    private static void ResumeGame()
    {
        AppContext.Instance.GamePause.Resume();
        UIService.Current?.Show(ScreenId.Hud);
    }

    private static void ReturnToMainMenu()
    {
        AppContext appContext = AppContext.Instance;
        appContext.GamePause.Resume();

        if (appContext.SceneLoader.LoadScene(SceneId.MainMenu))
        {
            appContext.Inventory.Clear();
        }
    }
}
