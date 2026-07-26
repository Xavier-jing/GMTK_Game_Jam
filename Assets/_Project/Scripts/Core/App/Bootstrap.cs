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
            LoadingScreen.EnsureExists();
            _ = LoadingScreen.Current?.HideAsync();
            return;
        }

        appContext.GamePause.Resume();

        string mainMenuPath = appContext.SceneLoader.GetScenePath(SceneId.MainMenu);
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(mainMenuPath);
        if (buildIndex < 0)
        {
            Debug.LogError($"[Bootstrap] MainMenu scene not in Build Settings: {mainMenuPath}");
            return;
        }

        BootVideoLoading bootVideo = Object.FindObjectOfType<BootVideoLoading>();
        bool useBootVideo = bootVideo != null && bootVideo.CanPlay;
        if (useBootVideo)
        {
            LoadingScreen.Current?.HideImmediately();
            Object.DontDestroyOnLoad(bootVideo.gameObject);
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError("[Bootstrap] Failed to start loading MainMenu.");
            return;
        }

        if (!useBootVideo)
        {
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            await (LoadingScreen.Current?.HideAsync() ?? Task.CompletedTask);
            return;
        }

        operation.allowSceneActivation = false;
        Task<bool> videoTask = bootVideo.PlayAsync();

        while (operation.progress < 0.9f)
        {
            await Task.Yield();
        }

        await videoTask;
        Debug.Log($"[Bootstrap] Boot video task completed. Result={videoTask.Result}, VideoPlayer.time={bootVideo.VideoTime:0.00}s, VideoPlayer.length={bootVideo.VideoLength:0.00}s, IsPrepared={bootVideo.IsPrepared}");
        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            await Task.Yield();
        }

        Object.Destroy(bootVideo.gameObject);
    }
}
