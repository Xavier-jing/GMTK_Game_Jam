using UnityEngine;

public sealed class MainMenuSceneController : MonoBehaviour
{
    [SerializeField]
    private ScreenId landingScreen = ScreenId.MainMenu;

    [SerializeField]
    private UIService uiService;

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
        appContext.GamePause.Resume();

        if (uiInput != null)
        {
            uiInput.EnableUINavigation();
            uiInput.OnCancel += HandleCancelPerformed;
        }

        if (uiService == null)
        {
            Debug.LogWarning($"MainMenuSceneController on '{name}' is missing a UIService reference.");
            return;
        }

        uiService.Show(landingScreen);
    }

    private void OnDestroy()
    {
        if (uiInput != null)
        {
            uiInput.OnCancel -= HandleCancelPerformed;
        }
    }

    private void HandleCancelPerformed()
    {
        if (uiService == null || uiService.CurrentScreenId != ScreenId.Settings)
        {
            return;
        }

        uiService.Get<SettingsScreen>(ScreenId.Settings).GoBack();
    }
}
