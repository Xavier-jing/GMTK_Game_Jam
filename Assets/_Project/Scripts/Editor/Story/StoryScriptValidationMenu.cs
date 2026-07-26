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
        Dictionary<string, bool> audioResourceResults =
            new Dictionary<string, bool>(StringComparer.Ordinal);
        List<string> allErrors = new List<string>();
        bool foundEndingSequenceCatalog = false;

        string[] assetGuids = AssetDatabase.FindAssets(
            "t:TextAsset",
            new[] { StoryAssetFolder });
        int jsonAssetCount = 0;

        foreach (string assetGuid in assetGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (!assetPath.EndsWith(
                    ".json",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            jsonAssetCount++;
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (asset == null)
            {
                allErrors.Add($"{assetPath}: could not load TextAsset.");
                continue;
            }

            if (string.Equals(
                    asset.name,
                    EndingSequenceCatalog.ResourceName,
                    StringComparison.Ordinal))
            {
                foundEndingSequenceCatalog = true;
                if (!EndingSequenceCatalog.TryParse(
                        asset.text,
                        out EndingSequenceCatalog _,
                        out string endingError))
                {
                    allErrors.Add($"{assetPath}: {endingError}");
                }

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
            ValidateAudioResources(
                graph,
                assetPath,
                audioResourceResults,
                allErrors);
        }

        ValidateCrossScriptTargets(graphs, sourcePaths, allErrors);
        ValidateInitialDialoguePortraits(graphs, sourcePaths, allErrors);

        if (!foundEndingSequenceCatalog)
        {
            allErrors.Add(
                $"{StoryAssetFolder}/{EndingSequenceCatalog.ResourceName}.json: " +
                "ending sequence configuration was not found.");
        }

        if (allErrors.Count > 0)
        {
            foreach (string error in allErrors)
            {
                Debug.LogError($"Story validation: {error}");
            }

            Debug.LogError(
                $"Story validation failed with {allErrors.Count} error(s) " +
                $"across {jsonAssetCount} JSON asset(s).");
            return;
        }

        Debug.Log(
            $"Story validation passed for {graphs.Count} story script(s) and " +
            $"the ending sequence catalog in '{StoryAssetFolder}'.");
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

    internal static void ValidateInitialDialoguePortraits(
        IReadOnlyDictionary<string, StoryGraph> graphs,
        IReadOnlyDictionary<string, string> sourcePaths,
        ICollection<string> errors)
    {
        foreach (KeyValuePair<string, StoryGraph> graphPair in graphs)
        {
            HashSet<string> visitedNodes =
                new HashSet<string>(StringComparer.Ordinal);
            ValidateFirstDialogueOnPaths(
                graphPair.Key,
                graphPair.Key,
                graphPair.Value.Document.StartNodeId,
                graphs,
                sourcePaths,
                visitedNodes,
                errors);
        }
    }

    private static void ValidateFirstDialogueOnPaths(
        string rootScriptId,
        string scriptId,
        string nodeId,
        IReadOnlyDictionary<string, StoryGraph> graphs,
        IReadOnlyDictionary<string, string> sourcePaths,
        ISet<string> visitedNodes,
        ICollection<string> errors)
    {
        string visitKey = $"{scriptId}\n{nodeId}";
        if (!visitedNodes.Add(visitKey) ||
            !graphs.TryGetValue(scriptId, out StoryGraph graph) ||
            !graph.TryGetNode(nodeId, out StoryNodeDefinition node))
        {
            return;
        }

        if (node.Type == StoryNodeType.Dialogue)
        {
            if (string.IsNullOrWhiteSpace(node.Data.PortraitId))
            {
                string sourcePath = sourcePaths.TryGetValue(
                    scriptId,
                    out string targetSourcePath)
                    ? targetSourcePath
                    : scriptId;
                errors.Add(
                    $"{sourcePath}: story '{rootScriptId}' can first reach Dialogue " +
                    $"'{scriptId}/{nodeId}' without a PortraitId.");
            }

            return;
        }

        if (node.Type == StoryNodeType.Action)
        {
            if (!string.IsNullOrEmpty(node.Data.Next))
            {
                ValidateFirstDialogueOnPaths(
                    rootScriptId,
                    scriptId,
                    node.Data.Next,
                    graphs,
                    sourcePaths,
                    visitedNodes,
                    errors);
            }

            return;
        }

        if (node.Type != StoryNodeType.Choice || node.Data.Choices == null)
        {
            return;
        }

        foreach (StoryChoiceData choice in node.Data.Choices)
        {
            if (choice == null)
            {
                continue;
            }

            ValidateFirstDialogueOnPaths(
                rootScriptId,
                choice.TargetScriptId,
                choice.TargetNodeId,
                graphs,
                sourcePaths,
                visitedNodes,
                errors);
        }
    }

    private static void ValidateAudioResources(
        StoryGraph graph,
        string sourcePath,
        IDictionary<string, bool> resourceResults,
        ICollection<string> errors)
    {
        foreach (StoryNodeDefinition node in graph.GetNodes())
        {
            string nodeLocation =
                $"{sourcePath}: {graph.Document.ScriptId}/{node.Data.Id}";
            ValidateAudioActions(
                node.Data.BeforeActions,
                $"{nodeLocation}/BeforeActions",
                resourceResults,
                errors);
            ValidateAudioActions(
                node.Data.AfterActions,
                $"{nodeLocation}/AfterActions",
                resourceResults,
                errors);
            ValidateAudioActions(
                node.Data.Actions,
                $"{nodeLocation}/Actions",
                resourceResults,
                errors);
        }
    }

    private static void ValidateAudioActions(
        StoryActionData[] actions,
        string location,
        IDictionary<string, bool> resourceResults,
        ICollection<string> errors)
    {
        if (actions == null)
        {
            return;
        }

        for (int index = 0; index < actions.Length; index++)
        {
            StoryActionData action = actions[index];
            if (!TryGetAudioResourcePath(action, out string resourcePath))
            {
                continue;
            }

            if (!resourceResults.TryGetValue(
                    resourcePath,
                    out bool resourceExists))
            {
                resourceExists =
                    Resources.Load<AudioClip>(resourcePath) != null;
                resourceResults.Add(resourcePath, resourceExists);
            }

            if (!resourceExists)
            {
                errors.Add(
                    $"{location}[{index}]/{action.Id}: AudioClip resource " +
                    $"'Resources/{resourcePath}' was not found or is not an AudioClip.");
            }
        }
    }

    private static bool TryGetAudioResourcePath(
        StoryActionData action,
        out string resourcePath)
    {
        resourcePath = string.Empty;
        if (action == null || action.Params == null)
        {
            return false;
        }

        if (string.Equals(
                action.Id,
                PlaySfxStoryAction.ActionId,
                StringComparison.Ordinal))
        {
            resourcePath =
                AudioService.GetSfxResourcePath(action.Params.StringValue);
            return true;
        }

        if (string.Equals(
                action.Id,
                SwitchBgmStoryAction.ActionId,
                StringComparison.Ordinal))
        {
            resourcePath =
                AudioService.GetBgmResourcePath(action.Params.StringValue);
            return true;
        }

        return false;
    }
}
