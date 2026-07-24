using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader : IDisposable
{
    private readonly Dictionary<SceneId, string> scenePaths = new Dictionary<SceneId, string>
    {
        { SceneId.Boot, "Assets/_Project/Scenes/Boot.unity" },
        { SceneId.MainMenu, "Assets/_Project/Scenes/MainMenu.unity" },
        { SceneId.Gameplay, "Assets/_Project/Scenes/GamePlay.unity" },
        { SceneId.Sandbox, "Assets/_Project/Scenes/SandBox.unity" },
    };

    private ISceneTransition transition;

    public bool IsLoading { get; private set; }

    public float LoadProgress { get; private set; }

    public SceneId? LoadingScene { get; private set; }

    public event Action<bool> LoadingStateChanged;

    public event Action<float> LoadProgressChanged;

    public event Action<SceneId> SceneLoadStarted;

    public event Action<SceneId> SceneLoaded;

    public SceneLoader()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    public void Dispose()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        transition = null;
    }

    public void RegisterTransition(ISceneTransition sceneTransition)
    {
        transition = sceneTransition ?? throw new ArgumentNullException(nameof(sceneTransition));
        transition.SetProgress(LoadProgress);
    }

    public void UnregisterTransition(ISceneTransition sceneTransition)
    {
        if (ReferenceEquals(transition, sceneTransition))
        {
            transition = null;
        }
    }

    public bool LoadScene(SceneId sceneId, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        if (IsLoading)
        {
            Debug.LogWarning(
                $"Ignored scene load request for '{sceneId}' because '{LoadingScene}' is already loading.");
            return false;
        }

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(GetScenePath(sceneId));
        if (buildIndex < 0)
        {
            throw new InvalidOperationException(
                $"Scene '{sceneId}' is not in Build Settings. Run Tools/Jam Template/Sync Build Settings in the Unity editor.");
        }

        BeginLoading(sceneId);
        _ = LoadSceneInternalAsync(sceneId, buildIndex, loadSceneMode);
        return true;
    }

    public bool IsActiveScene(SceneId sceneId)
    {
        return TryGetSceneId(SceneManager.GetActiveScene().path, out SceneId activeSceneId) && activeSceneId == sceneId;
    }

    public string GetScenePath(SceneId sceneId)
    {
        if (scenePaths.TryGetValue(sceneId, out string scenePath))
        {
            return scenePath;
        }

        throw new ArgumentOutOfRangeException(nameof(sceneId), sceneId, "Unknown scene id.");
    }

    public bool TryGetSceneId(string scenePath, out SceneId sceneId)
    {
        string normalizedPath = NormalizeScenePath(scenePath);

        foreach (KeyValuePair<SceneId, string> pair in scenePaths)
        {
            if (string.Equals(NormalizeScenePath(pair.Value), normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                sceneId = pair.Key;
                return true;
            }
        }

        sceneId = default;
        return false;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (TryGetSceneId(scene.path, out SceneId sceneId))
        {
            SceneLoaded?.Invoke(sceneId);
        }
    }

    private async Task LoadSceneInternalAsync(SceneId sceneId, int buildIndex, LoadSceneMode loadSceneMode)
    {
        try
        {
            ISceneTransition activeTransition = GetActiveTransition();
            if (activeTransition != null)
            {
                await activeTransition.ShowAsync();
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex, loadSceneMode);
            if (operation == null)
            {
                throw new InvalidOperationException($"Failed to start loading scene '{sceneId}'.");
            }

            while (!operation.isDone)
            {
                float rawProgress = operation.progress;
                float normalizedProgress = rawProgress >= 0.9f
                    ? 1f
                    : rawProgress / 0.9f;

                SetLoadProgress(normalizedProgress);
                await Task.Delay(10);
            }

            SetLoadProgress(1f);
            await Task.Delay(10);

            activeTransition = GetActiveTransition();
            if (activeTransition != null)
            {
                await activeTransition.HideAsync();
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            await TryHideTransitionAsync();
        }
        finally
        {
            EndLoading();
        }
    }

    private async Task TryHideTransitionAsync()
    {
        try
        {
            ISceneTransition activeTransition = GetActiveTransition();
            if (activeTransition != null)
            {
                await activeTransition.HideAsync();
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private void BeginLoading(SceneId sceneId)
    {
        IsLoading = true;
        LoadingScene = sceneId;
        SetLoadProgress(0f, forceNotification: true);
        LoadingStateChanged?.Invoke(true);
        SceneLoadStarted?.Invoke(sceneId);
    }

    private void EndLoading()
    {
        LoadingScene = null;
        IsLoading = false;
        LoadingStateChanged?.Invoke(false);
    }

    private void SetLoadProgress(float normalizedProgress, bool forceNotification = false)
    {
        float clampedProgress = Mathf.Clamp01(normalizedProgress);
        if (!forceNotification && Mathf.Approximately(LoadProgress, clampedProgress))
        {
            return;
        }

        LoadProgress = clampedProgress;
        GetActiveTransition()?.SetProgress(LoadProgress);
        LoadProgressChanged?.Invoke(LoadProgress);
    }

    private ISceneTransition GetActiveTransition()
    {
        if (transition is UnityEngine.Object unityObject && unityObject == null)
        {
            transition = null;
        }

        return transition;
    }

    private static string NormalizeScenePath(string scenePath)
    {
        return scenePath.Replace('\\', '/');
    }
}
