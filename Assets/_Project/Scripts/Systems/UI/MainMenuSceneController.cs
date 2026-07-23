using UnityEngine;

public sealed class MainMenuSceneController : MonoBehaviour
{
    [SerializeField]
    private ScreenId landingScreen = ScreenId.MainMenu;

    [SerializeField]
    private UIService uiService;

    private void Awake()
    {
        if (uiService == null)
        {
            uiService = GetComponentInChildren<UIService>(true);
        }
    }

    private void Start()
    {
        AppContext.Instance.GamePause.Resume();

        if (uiService == null)
        {
            Debug.LogWarning($"MainMenuSceneController on '{name}' is missing a UIService reference.");
            return;
        }

        uiService.Show(landingScreen);
    }
}
