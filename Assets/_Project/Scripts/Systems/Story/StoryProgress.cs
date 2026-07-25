using System;
using System.Collections.Generic;

public sealed class StoryProgress
{
    private readonly Dictionary<string, bool> flags =
        new Dictionary<string, bool>(StringComparer.Ordinal);
    private readonly HashSet<string> completedScripts =
        new HashSet<string>(StringComparer.Ordinal);

    public bool GetFlag(string key)
    {
        return !string.IsNullOrEmpty(key) &&
               flags.TryGetValue(key, out bool value) &&
               value;
    }

    public bool TryGetFlag(string key, out bool value)
    {
        if (string.IsNullOrEmpty(key))
        {
            value = false;
            return false;
        }

        return flags.TryGetValue(key, out value);
    }

    public void SetFlag(string key, bool value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Story flag key cannot be empty.", nameof(key));
        }

        flags[key] = value;
    }

    public bool IsScriptCompleted(string scriptId)
    {
        return !string.IsNullOrEmpty(scriptId) && completedScripts.Contains(scriptId);
    }

    public void MarkScriptCompleted(string scriptId)
    {
        if (string.IsNullOrWhiteSpace(scriptId))
        {
            throw new ArgumentException("Story script id cannot be empty.", nameof(scriptId));
        }

        completedScripts.Add(scriptId);
    }

    public void Clear()
    {
        flags.Clear();
        completedScripts.Clear();
    }
}
