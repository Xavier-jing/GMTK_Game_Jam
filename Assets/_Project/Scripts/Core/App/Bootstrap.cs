using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeApplication()
    {
        AppContext.EnsureExists();

        SceneManager.sceneLoaded -= HandleInitialSceneLoaded;
        SceneManager.sceneLoaded += HandleInitialSceneLoaded;
    }

    private static async void HandleInitialSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        SceneManager.sceneLoaded -= HandleInitialSceneLoaded;

        AppContext appContext = AppContext.EnsureExists();
        if (!appContext.SceneLoader.TryGetSceneId(scene.path, out SceneId sceneId) ||
            sceneId != SceneId.Boot)
        {
            // 直接运行了非 Boot 场景（如从 MainMenu 启动调试），淡出黑屏
            LoadingScreen.Current?.HideAsync();
            return;
        }

        // Boot 场景：不走 SceneLoader（不需要进度条），直接异步加载 MainMenu
        appContext.GamePause.Resume();

        string mainMenuPath = appContext.SceneLoader.GetScenePath(SceneId.MainMenu);
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(mainMenuPath);
        if (buildIndex < 0)
        {
            Debug.LogError($"[Bootstrap] MainMenu scene not in Build Settings: {mainMenuPath}");
            return;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError("[Bootstrap] Failed to start loading MainMenu.");
            return;
        }

        while (!operation.isDone)
        {
            await Task.Yield();
        }

        // MainMenu 加载完毕，黑屏淡出
        await (LoadingScreen.Current?.HideAsync() ?? Task.CompletedTask);
    }
}
