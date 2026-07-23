#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class BuildSettingsSceneSync
{
    private static readonly string[] RequiredScenePaths =
    {
        "Assets/_Project/Scenes/Boot.unity",
        "Assets/_Project/Scenes/MainMenu.unity",
        "Assets/_Project/Scenes/GamePlay.unity",
        "Assets/_Project/Scenes/SandBox.unity",
    };

    static BuildSettingsSceneSync()
    {
        EditorApplication.delayCall += EnsureRequiredScenes;
    }

    [MenuItem("Tools/Jam Template/Sync Build Settings")]
    public static void EnsureRequiredScenes()
    {
        EditorApplication.delayCall -= EnsureRequiredScenes;

        List<EditorBuildSettingsScene> desiredScenes = new List<EditorBuildSettingsScene>();
        HashSet<string> requiredPaths = new HashSet<string>(RequiredScenePaths);

        foreach (string scenePath in RequiredScenePaths)
        {
            if (!File.Exists(scenePath))
            {
                UnityEngine.Debug.LogWarning($"BuildSettingsSceneSync skipped missing scene: {scenePath}");
                continue;
            }

            desiredScenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (requiredPaths.Contains(scene.path))
            {
                continue;
            }

            desiredScenes.Add(scene);
        }

        if (HasSameLayout(EditorBuildSettings.scenes, desiredScenes))
        {
            return;
        }

        EditorBuildSettings.scenes = desiredScenes.ToArray();
        UnityEngine.Debug.Log("Synced V0 scenes into Build Settings.");
    }

    private static bool HasSameLayout(EditorBuildSettingsScene[] currentScenes, List<EditorBuildSettingsScene> desiredScenes)
    {
        if (currentScenes.Length != desiredScenes.Count)
        {
            return false;
        }

        for (int index = 0; index < currentScenes.Length; index++)
        {
            EditorBuildSettingsScene currentScene = currentScenes[index];
            EditorBuildSettingsScene desiredScene = desiredScenes[index];

            if (currentScene.path != desiredScene.path || currentScene.enabled != desiredScene.enabled)
            {
                return false;
            }
        }

        return true;
    }
}
#endif
