using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class StoryScriptValidationMenu
{
    private const string StoryAssetFolder = "Assets/_Project/Resources/Story";

    [MenuItem("Tools/Jam Template/Validate Story Scripts")]
    public static void ValidateStoryScripts()
    {
        if (!AssetDatabase.IsValidFolder(StoryAssetFolder))
        {
            Debug.LogWarning(
                $"Story validation skipped because '{StoryAssetFolder}' does not exist.");
            return;
        }

        StoryLoader loader = new StoryLoader();
        StoryActionRegistry actionRegistry = new StoryActionRegistry();
        StoryConditionRegistry conditionRegistry = new StoryConditionRegistry();
        StoryValidator validator =
            new StoryValidator(actionRegistry, conditionRegistry);

        Dictionary<string, StoryGraph> graphs =
            new Dictionary<string, StoryGraph>(StringComparer.Ordinal);
        Dictionary<string, string> sourcePaths =
            new Dictionary<string, string>(StringComparer.Ordinal);
        List<string> allErrors = new List<string>();

        string[] assetGuids = AssetDatabase.FindAssets(
            "t:TextAsset",
            new[] { StoryAssetFolder });

        foreach (string assetGuid in assetGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (asset == null)
            {
                allErrors.Add($"{assetPath}: could not load TextAsset.");
                continue;
            }

            StoryLoadResult loadResult = loader.Parse(asset.text, assetPath);
            if (!loadResult.Succeeded)
            {
                allErrors.Add($"{assetPath}: {loadResult.Error}");
                continue;
            }

            StoryDocumentData document = loadResult.Document;
            if (!string.Equals(asset.name, document.ScriptId, StringComparison.Ordinal))
            {
                allErrors.Add(
                    $"{assetPath}: file name '{asset.name}' must match ScriptId " +
                    $"'{document.ScriptId}'.");
            }

            if (!validator.TryValidate(
                    document,
                    out StoryGraph graph,
                    out List<string> validationErrors))
            {
                foreach (string validationError in validationErrors)
                {
                    allErrors.Add($"{assetPath}: {validationError}");
                }

                continue;
            }

            if (graphs.TryGetValue(document.ScriptId, out StoryGraph _))
            {
                allErrors.Add(
                    $"{assetPath}: duplicate ScriptId '{document.ScriptId}', " +
                    $"already declared by '{sourcePaths[document.ScriptId]}'.");
                continue;
            }

            graphs.Add(document.ScriptId, graph);
            sourcePaths.Add(document.ScriptId, assetPath);
        }

        ValidateCrossScriptTargets(graphs, sourcePaths, allErrors);

        if (allErrors.Count > 0)
        {
            foreach (string error in allErrors)
            {
                Debug.LogError($"Story validation: {error}");
            }

            Debug.LogError(
                $"Story validation failed with {allErrors.Count} error(s) " +
                $"across {assetGuids.Length} TextAsset(s).");
            return;
        }

        Debug.Log(
            $"Story validation passed for {graphs.Count} script(s) " +
            $"in '{StoryAssetFolder}'.");
    }

    private static void ValidateCrossScriptTargets(
        IReadOnlyDictionary<string, StoryGraph> graphs,
        IReadOnlyDictionary<string, string> sourcePaths,
        ICollection<string> errors)
    {
        foreach (KeyValuePair<string, StoryGraph> graphPair in graphs)
        {
            StoryGraph graph = graphPair.Value;
            string sourcePath = sourcePaths[graphPair.Key];

            foreach (StoryNodeDefinition node in graph.GetNodes())
            {
                if (node.Type != StoryNodeType.Choice || node.Data.Choices == null)
                {
                    continue;
                }

                for (int index = 0; index < node.Data.Choices.Length; index++)
                {
                    StoryChoiceData choice = node.Data.Choices[index];
                    if (choice == null)
                    {
                        continue;
                    }

                    string location =
                        $"{sourcePath}: {graph.Document.ScriptId}/{node.Data.Id}/Choices[{index}]";

                    if (!graphs.TryGetValue(
                            choice.TargetScriptId,
                            out StoryGraph targetGraph))
                    {
                        errors.Add(
                            $"{location}: target script '{choice.TargetScriptId}' was not found.");
                        continue;
                    }

                    if (!targetGraph.ContainsNode(choice.TargetNodeId))
                    {
                        errors.Add(
                            $"{location}: target node " +
                            $"'{choice.TargetScriptId}/{choice.TargetNodeId}' was not found.");
                    }
                }
            }
        }
    }
}
