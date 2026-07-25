using System;
using System.Collections.Generic;

public sealed class StoryValidator
{
    public const int SupportedVersion = 1;

    private readonly StoryActionRegistry actionRegistry;
    private readonly StoryConditionRegistry conditionRegistry;

    public StoryValidator(
        StoryActionRegistry actionRegistry,
        StoryConditionRegistry conditionRegistry)
    {
        this.actionRegistry =
            actionRegistry ?? throw new ArgumentNullException(nameof(actionRegistry));
        this.conditionRegistry =
            conditionRegistry ?? throw new ArgumentNullException(nameof(conditionRegistry));
    }

    public bool TryValidate(
        StoryDocumentData document,
        out StoryGraph graph,
        out List<string> errors)
    {
        errors = new List<string>();
        graph = null;

        if (document == null)
        {
            errors.Add("Story document is null.");
            return false;
        }

        ValidateDocumentHeader(document, errors);

        Dictionary<string, StoryNodeDefinition> nodes =
            new Dictionary<string, StoryNodeDefinition>(StringComparer.Ordinal);

        if (document.Nodes == null || document.Nodes.Length == 0)
        {
            errors.Add("Nodes must contain at least one node.");
            return false;
        }

        BuildNodeLookup(document, nodes, errors);
        ValidateStartNode(document, nodes, errors);

        foreach (StoryNodeDefinition node in nodes.Values)
        {
            ValidateNode(document, node, nodes, errors);
        }

        ValidateActionOnlyCycles(document, nodes, errors);

        if (errors.Count > 0)
        {
            return false;
        }

        graph = new StoryGraph(document, nodes);
        return true;
    }

    public static bool IsValidId(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            bool valid =
                character >= 'a' && character <= 'z' ||
                character >= 'A' && character <= 'Z' ||
                character >= '0' && character <= '9' ||
                character == '_' ||
                character == '-';

            if (!valid)
            {
                return false;
            }
        }

        return true;
    }

    public static bool TryParseUnavailableMode(
        string value,
        out StoryUnavailableMode mode)
    {
        if (string.IsNullOrEmpty(value))
        {
            mode = StoryUnavailableMode.Disabled;
            return true;
        }

        return Enum.TryParse(value, true, out mode);
    }

    private static void ValidateDocumentHeader(
        StoryDocumentData document,
        ICollection<string> errors)
    {
        if (document.Version != SupportedVersion)
        {
            errors.Add(
                $"Version must be {SupportedVersion}, but was {document.Version}.");
        }

        if (!IsValidId(document.ScriptId))
        {
            errors.Add(
                $"ScriptId '{document.ScriptId}' must contain only letters, numbers, underscores, or hyphens.");
        }

        if (!IsValidId(document.StartNodeId))
        {
            errors.Add(
                $"StartNodeId '{document.StartNodeId}' must be a valid id.");
        }
    }

    private static void BuildNodeLookup(
        StoryDocumentData document,
        IDictionary<string, StoryNodeDefinition> nodes,
        ICollection<string> errors)
    {
        for (int index = 0; index < document.Nodes.Length; index++)
        {
            StoryNodeData nodeData = document.Nodes[index];
            if (nodeData == null)
            {
                errors.Add($"Nodes[{index}] is null.");
                continue;
            }

            if (!IsValidId(nodeData.Id))
            {
                errors.Add(
                    $"Nodes[{index}].Id '{nodeData.Id}' must be a valid id.");
                continue;
            }

            if (!Enum.TryParse(nodeData.Type, true, out StoryNodeType nodeType))
            {
                errors.Add(
                    $"{document.ScriptId}/{nodeData.Id}: unknown node type '{nodeData.Type}'.");
                continue;
            }

            if (nodes.ContainsKey(nodeData.Id))
            {
                errors.Add(
                    $"{document.ScriptId}: duplicate node id '{nodeData.Id}'.");
                continue;
            }

            nodes.Add(nodeData.Id, new StoryNodeDefinition(nodeData, nodeType));
        }
    }

    private static void ValidateStartNode(
        StoryDocumentData document,
        IReadOnlyDictionary<string, StoryNodeDefinition> nodes,
        ICollection<string> errors)
    {
        if (IsValidId(document.StartNodeId) &&
            !nodes.ContainsKey(document.StartNodeId))
        {
            errors.Add(
                $"{document.ScriptId}: start node '{document.StartNodeId}' was not found.");
        }
    }

    private void ValidateNode(
        StoryDocumentData document,
        StoryNodeDefinition node,
        IReadOnlyDictionary<string, StoryNodeDefinition> nodes,
        ICollection<string> errors)
    {
        string location = $"{document.ScriptId}/{node.Data.Id}";

        if (!string.IsNullOrEmpty(node.Data.ActorId) &&
            !IsValidId(node.Data.ActorId))
        {
            errors.Add($"{location}: ActorId '{node.Data.ActorId}' is invalid.");
        }

        switch (node.Type)
        {
            case StoryNodeType.Dialogue:
                ValidateDialogueNode(node.Data, location, nodes, errors);
                break;
            case StoryNodeType.Action:
                ValidateActionNode(node.Data, location, nodes, errors);
                break;
            case StoryNodeType.Choice:
                ValidateChoiceNode(document, node.Data, location, nodes, errors);
                break;
            case StoryNodeType.End:
                break;
            default:
                errors.Add($"{location}: unsupported node type '{node.Type}'.");
                break;
        }
    }

    private void ValidateDialogueNode(
        StoryNodeData node,
        string location,
        IReadOnlyDictionary<string, StoryNodeDefinition> nodes,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(node.Dialog))
        {
            errors.Add($"{location}: Dialogue node requires Dialog.");
        }

        ValidateLocalNext(node.Next, location, nodes, errors);
        ValidateActions(node.BeforeActions, $"{location}/BeforeActions", errors);
        ValidateActions(node.AfterActions, $"{location}/AfterActions", errors);
    }

    private void ValidateActionNode(
        StoryNodeData node,
        string location,
        IReadOnlyDictionary<string, StoryNodeDefinition> nodes,
        ICollection<string> errors)
    {
        if (node.Actions == null || node.Actions.Length == 0)
        {
            errors.Add($"{location}: Action node requires at least one action.");
        }
        else
        {
            ValidateActions(node.Actions, $"{location}/Actions", errors);
        }

        ValidateLocalNext(node.Next, location, nodes, errors);
    }

    private void ValidateChoiceNode(
        StoryDocumentData document,
        StoryNodeData node,
        string location,
        IReadOnlyDictionary<string, StoryNodeDefinition> nodes,
        ICollection<string> errors)
    {
        if (node.Choices == null || node.Choices.Length == 0)
        {
            errors.Add($"{location}: Choice node requires at least one choice.");
            return;
        }

        for (int index = 0; index < node.Choices.Length; index++)
        {
            StoryChoiceData choice = node.Choices[index];
            string choiceLocation = $"{location}/Choices[{index}]";

            if (choice == null)
            {
                errors.Add($"{choiceLocation}: choice is null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(choice.Dialog))
            {
                errors.Add($"{choiceLocation}: Dialog is required.");
            }

            if (!IsValidId(choice.TargetScriptId))
            {
                errors.Add(
                    $"{choiceLocation}: TargetScriptId '{choice.TargetScriptId}' is invalid.");
            }

            if (!IsValidId(choice.TargetNodeId))
            {
                errors.Add(
                    $"{choiceLocation}: TargetNodeId '{choice.TargetNodeId}' is invalid.");
            }

            if (string.Equals(
                    choice.TargetScriptId,
                    document.ScriptId,
                    StringComparison.Ordinal) &&
                IsValidId(choice.TargetNodeId) &&
                !nodes.ContainsKey(choice.TargetNodeId))
            {
                errors.Add(
                    $"{choiceLocation}: target node '{choice.TargetNodeId}' was not found.");
            }

            ValidateCondition(choice.Condition, choiceLocation, errors);
        }
    }

    private void ValidateActions(
        StoryActionData[] actions,
        string location,
        ICollection<string> errors)
    {
        if (actions == null)
        {
            return;
        }

        for (int index = 0; index < actions.Length; index++)
        {
            StoryActionData action = actions[index];
            string actionLocation = $"{location}[{index}]";

            if (action == null)
            {
                errors.Add($"{actionLocation}: action is null.");
                continue;
            }

            if (!actionRegistry.TryGet(action.Id, out IStoryActionHandler handler))
            {
                errors.Add(
                    $"{actionLocation}: unknown action '{action.Id}'.");
                continue;
            }

            if (!handler.Validate(action.Params, out string error))
            {
                errors.Add(
                    $"{actionLocation}/{action.Id}: {error}");
            }
        }
    }

    private void ValidateCondition(
        StoryConditionData condition,
        string location,
        ICollection<string> errors)
    {
        if (condition == null)
        {
            return;
        }

        if (!conditionRegistry.TryGet(
                condition.Id,
                out IStoryConditionHandler handler))
        {
            errors.Add(
                $"{location}: unknown condition '{condition.Id}'.");
            return;
        }

        if (!TryParseUnavailableMode(
                condition.UnavailableMode,
                out StoryUnavailableMode _))
        {
            errors.Add(
                $"{location}/{condition.Id}: UnavailableMode must be 'Hidden' or 'Disabled'.");
        }

        if (!handler.Validate(condition.Params, out string error))
        {
            errors.Add(
                $"{location}/{condition.Id}: {error}");
        }
    }

    private static void ValidateLocalNext(
        string next,
        string location,
        IReadOnlyDictionary<string, StoryNodeDefinition> nodes,
        ICollection<string> errors)
    {
        if (!IsValidId(next))
        {
            errors.Add($"{location}: Next '{next}' must be a valid node id.");
            return;
        }

        if (!nodes.ContainsKey(next))
        {
            errors.Add($"{location}: next node '{next}' was not found.");
        }
    }

    private static void ValidateActionOnlyCycles(
        StoryDocumentData document,
        IReadOnlyDictionary<string, StoryNodeDefinition> nodes,
        ICollection<string> errors)
    {
        HashSet<string> reportedCycles =
            new HashSet<string>(StringComparer.Ordinal);

        foreach (StoryNodeDefinition node in nodes.Values)
        {
            if (node.Type != StoryNodeType.Action)
            {
                continue;
            }

            HashSet<string> visited =
                new HashSet<string>(StringComparer.Ordinal);
            StoryNodeDefinition cursor = node;

            while (cursor.Type == StoryNodeType.Action &&
                   !string.IsNullOrEmpty(cursor.Data.Next) &&
                   nodes.TryGetValue(cursor.Data.Next, out StoryNodeDefinition next))
            {
                if (!visited.Add(cursor.Data.Id))
                {
                    if (reportedCycles.Add(cursor.Data.Id))
                    {
                        errors.Add(
                            $"{document.ScriptId}/{cursor.Data.Id}: " +
                            "action-only node cycle would never wait for player input.");
                    }

                    break;
                }

                cursor = next;
            }
        }
    }
}
