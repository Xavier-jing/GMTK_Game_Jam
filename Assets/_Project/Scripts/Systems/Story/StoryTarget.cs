using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StoryTarget : MonoBehaviour
{
    [SerializeField]
    private string targetId;

    public string TargetId => targetId;
}

public sealed class StoryTargetRegistry
{
    private readonly Dictionary<string, StoryTarget> targets =
        new Dictionary<string, StoryTarget>(StringComparer.Ordinal);
    private readonly List<string> errors = new List<string>();

    public StoryTargetRegistry(IEnumerable<StoryTarget> storyTargets)
    {
        if (storyTargets == null)
        {
            return;
        }

        foreach (StoryTarget target in storyTargets)
        {
            if (target == null)
            {
                continue;
            }

            if (!StoryValidator.IsValidId(target.TargetId))
            {
                errors.Add(
                    $"StoryTarget on '{target.name}' has invalid target id '{target.TargetId}'.");
                continue;
            }

            if (targets.TryGetValue(target.TargetId, out StoryTarget existing))
            {
                errors.Add(
                    $"Story target id '{target.TargetId}' is duplicated by " +
                    $"'{existing.name}' and '{target.name}'.");
                continue;
            }

            targets.Add(target.TargetId, target);
        }
    }

    public IReadOnlyList<string> Errors => errors;

    public bool TryGet(string targetId, out StoryTarget target)
    {
        if (!targets.TryGetValue(targetId, out target))
        {
            return false;
        }

        if (target != null)
        {
            return true;
        }

        targets.Remove(targetId);
        return false;
    }
}
