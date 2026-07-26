using System;
using System.Collections.Generic;

public enum StoryNodeType
{
    Dialogue,
    Action,
    Choice,
    End
}

public enum StoryUnavailableMode
{
    Hidden,
    Disabled
}

public enum StoryRunnerState
{
    Idle,
    Loading,
    ShowingDialogue,
    WaitingForAdvance,
    ExecutingAction,
    WaitingForChoice,
    Completed,
    Canceled,
    Faulted
}

public static class StoryPortraitIdMap
{
    public static string ResolveBindingId(string portraitId)
    {
        switch (portraitId)
        {
            case "0":
            case "1":
                return "face01";
            case "2":
                return "face02";
            case "3":
                return "face03";
            case "4":
                return "face04";
            case "5":
                return "face05";
            default:
                return portraitId ?? string.Empty;
        }
    }
}

public static class StoryCgId
{
    public const int Hide = 0;

    public static bool TryParse(string cgId, out int cgNumber)
    {
        cgNumber = 0;
        if (string.IsNullOrEmpty(cgId) ||
            (cgId.Length > 1 && cgId[0] == '0'))
        {
            return false;
        }

        int parsedNumber = 0;
        for (int index = 0; index < cgId.Length; index++)
        {
            char character = cgId[index];
            if (character < '0' || character > '9')
            {
                return false;
            }

            int digit = character - '0';
            if (parsedNumber > (int.MaxValue - digit) / 10)
            {
                return false;
            }

            parsedNumber = parsedNumber * 10 + digit;
        }

        cgNumber = parsedNumber;
        return true;
    }
}

[Serializable]
public sealed class StoryDocumentData
{
    public int Version;
    public string ScriptId;
    public string StartNodeId;
    public StoryNodeData[] Nodes;
}

[Serializable]
public sealed class StoryNodeData
{
    public string Id;
    public string Type;
    public string ActorId;
    public string PortraitId;
    public string CgId;
    public string Dialog;
    public string[] DialogOptions;
    public StoryActionData[] BeforeActions;
    public StoryActionData[] AfterActions;
    public StoryActionData[] Actions;
    public StoryChoiceData[] Choices;
    public string Next;
    public string Result;
}

[Serializable]
public sealed class StoryActionData
{
    public string Id;
    public StoryActionParams Params;
}

[Serializable]
public sealed class StoryConditionData
{
    public string Id;
    public StoryActionParams Params;
    public string UnavailableMode;
}

[Serializable]
public sealed class StoryChoiceData
{
    public string Dialog;
    public string TargetScriptId;
    public string TargetNodeId;
    public StoryConditionData Condition;
}

[Serializable]
public sealed class StoryActionParams
{
    public string Key;
    public string StringValue;
    public bool BoolValue;
    public int IntValue;
    public float FloatValue;
    public string TargetId;
}

public sealed class StoryNodeDefinition
{
    public StoryNodeDefinition(StoryNodeData data, StoryNodeType type)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Type = type;
    }

    public StoryNodeData Data { get; }

    public StoryNodeType Type { get; }
}

public sealed class StoryGraph
{
    private readonly Dictionary<string, StoryNodeDefinition> nodes;

    public StoryGraph(
        StoryDocumentData document,
        Dictionary<string, StoryNodeDefinition> nodes)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        this.nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
    }

    public StoryDocumentData Document { get; }

    public bool TryGetNode(string nodeId, out StoryNodeDefinition node)
    {
        return nodes.TryGetValue(nodeId, out node);
    }

    public bool ContainsNode(string nodeId)
    {
        return nodes.ContainsKey(nodeId);
    }

    public IEnumerable<StoryNodeDefinition> GetNodes()
    {
        return nodes.Values;
    }
}
