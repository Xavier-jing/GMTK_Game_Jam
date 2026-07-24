using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuScreen : ScreenBase
{
    [SerializeField]
    private Button sandboxButton;

    [SerializeField]
    private SceneId sandboxScene = SceneId.Sandbox;

    [SerializeField]
    private Button startButton;

    [SerializeField]
    private SceneId startScene = SceneId.Gameplay;

    [SerializeField]
    private Button SettingsButton;

    [SerializeField]
    private Button exitButton;

    private void Awake()
    {
        BindButtons();
    }

    private void OnEnable()
    {
        BindButtons();
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartGameClicked);
        }

        if (sandboxButton != null)
        {
            sandboxButton.onClick.RemoveListener(HandleSandboxClicked);
        }
    }

    private void BindButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartGameClicked);
            startButton.onClick.AddListener(HandleStartGameClicked);
        }

        if (sandboxButton != null)
        {
            sandboxButton.onClick.RemoveListener(HandleSandboxClicked);
            sandboxButton.onClick.AddListener(HandleSandboxClicked);
        }

        if (SettingsButton != null)
        {
            SettingsButton.onClick.RemoveListener(HandleSettingsClicked);
            SettingsButton.onClick.AddListener(HandleSettingsClicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(HandleExitClicked);
            exitButton.onClick.AddListener(HandleExitClicked);
        }
    }

    private void HandleStartGameClicked()
    {
        AppContext.Instance.SceneLoader.LoadScene(startScene);
    }

    private void HandleSandboxClicked()
    {
        AppContext.Instance.SceneLoader.LoadScene(sandboxScene);
    }

    private void HandleSettingsClicked()
    {
        Owner.Get<SettingsScreen>(ScreenId.Settings).OpenFrom(ScreenId.MainMenu);
    }

    private void HandleExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}