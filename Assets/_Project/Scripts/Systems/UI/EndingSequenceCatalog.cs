using System;
using System.Collections.Generic;
using UnityEngine;

public static class EndingSequenceId
{
    public const string Truth = "EVT_TRUTH";
    public const string EndingOne = "END_01";
    public const string EndingTwo = "END_02";
    public const string EndingThree = "END_03";
}

[Serializable]
public sealed class EndingSequenceCatalogData
{
    public int Version;
    public EndingSequenceData[] Sequences;
}

[Serializable]
public sealed class EndingSequenceData
{
    public string Id;
    public string[] Lines;
}

public sealed class EndingSequenceCatalog
{
    public const int SupportedVersion = 1;
    public const string ResourceName = "ending_sequences";
    public const string ResourcePath = "Story/" + ResourceName;

    private static readonly Dictionary<string, int> ExpectedLineCounts =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { EndingSequenceId.Truth, 5 },
            { EndingSequenceId.EndingOne, 2 },
            { EndingSequenceId.EndingTwo, 5 },
            { EndingSequenceId.EndingThree, 6 }
        };

    private readonly Dictionary<string, EndingSequenceData> sequences;

    private EndingSequenceCatalog(
        Dictionary<string, EndingSequenceData> sequences)
    {
        this.sequences = sequences;
    }

    public static bool TryLoad(
        out EndingSequenceCatalog catalog,
        out string error)
    {
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            catalog = null;
            error =
                $"Ending sequence configuration was not found at " +
                $"'Resources/{ResourcePath}.json'.";
            return false;
        }

        return TryParse(asset.text, out catalog, out error);
    }

    public static bool TryParse(
        string json,
        out EndingSequenceCatalog catalog,
        out string error)
    {
        catalog = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Ending sequence configuration is empty.";
            return false;
        }

        EndingSequenceCatalogData data;
        try
        {
            data = JsonUtility.FromJson<EndingSequenceCatalogData>(json);
        }
        catch (ArgumentException exception)
        {
            error =
                $"Ending sequence configuration contains invalid JSON: " +
                $"{exception.Message}";
            return false;
        }

        if (data == null)
        {
            error = "Ending sequence configuration could not be deserialized.";
            return false;
        }

        if (data.Version != SupportedVersion)
        {
            error =
                $"Ending sequence version '{data.Version}' is unsupported. " +
                $"Expected version '{SupportedVersion}'.";
            return false;
        }

        if (data.Sequences == null)
        {
            error = "Ending sequence configuration has no Sequences array.";
            return false;
        }

        Dictionary<string, EndingSequenceData> parsedSequences =
            new Dictionary<string, EndingSequenceData>(StringComparer.Ordinal);

        for (int index = 0; index < data.Sequences.Length; index++)
        {
            EndingSequenceData sequence = data.Sequences[index];
            if (sequence == null)
            {
                error = $"Sequences[{index}] is null.";
                return false;
            }

            string id = sequence.Id != null ? sequence.Id.Trim() : string.Empty;
            if (!ExpectedLineCounts.TryGetValue(id, out int expectedLineCount))
            {
                error =
                    $"Sequences[{index}] has unknown Id '{sequence.Id}'.";
                return false;
            }

            if (parsedSequences.ContainsKey(id))
            {
                error = $"Ending sequence Id '{id}' is duplicated.";
                return false;
            }

            if (sequence.Lines == null ||
                sequence.Lines.Length != expectedLineCount)
            {
                int actualLineCount =
                    sequence.Lines != null ? sequence.Lines.Length : 0;
                error =
                    $"Ending sequence '{id}' must contain " +
                    $"{expectedLineCount} line(s), but found {actualLineCount}.";
                return false;
            }

            for (int lineIndex = 0;
                 lineIndex < sequence.Lines.Length;
                 lineIndex++)
            {
                string line = sequence.Lines[lineIndex];
                if (string.IsNullOrWhiteSpace(line))
                {
                    error =
                        $"Ending sequence '{id}' contains an empty line at " +
                        $"index {lineIndex}.";
                    return false;
                }

                sequence.Lines[lineIndex] = line.Trim();
            }

            sequence.Id = id;
            parsedSequences.Add(id, sequence);
        }

        foreach (KeyValuePair<string, int> expected in ExpectedLineCounts)
        {
            if (!parsedSequences.ContainsKey(expected.Key))
            {
                error =
                    $"Ending sequence configuration is missing " +
                    $"'{expected.Key}'.";
                return false;
            }
        }

        catalog = new EndingSequenceCatalog(parsedSequences);
        error = string.Empty;
        return true;
    }

    public bool TryGet(string sequenceId, out EndingSequenceData sequence)
    {
        if (string.IsNullOrEmpty(sequenceId))
        {
            sequence = null;
            return false;
        }

        return sequences.TryGetValue(sequenceId, out sequence);
    }
}

public static class EndingSequenceFlow
{
    private static readonly string[] TruthFlow =
    {
        EndingSequenceId.Truth,
        EndingSequenceId.EndingOne
    };

    private static readonly string[] EndingOneFlow =
    {
        EndingSequenceId.EndingOne
    };

    private static readonly string[] EndingTwoFlow =
    {
        EndingSequenceId.EndingTwo
    };

    private static readonly string[] EndingThreeFlow =
    {
        EndingSequenceId.EndingThree
    };

    public static bool TryGetSequenceIds(
        RunEndReason reason,
        out IReadOnlyList<string> sequenceIds)
    {
        switch (reason)
        {
            case RunEndReason.TruthRevealed:
                sequenceIds = TruthFlow;
                return true;

            case RunEndReason.TurnsExhausted:
            case RunEndReason.EndingOne:
                sequenceIds = EndingOneFlow;
                return true;

            case RunEndReason.EndingTwo:
                sequenceIds = EndingTwoFlow;
                return true;

            case RunEndReason.EndingThree:
                sequenceIds = EndingThreeFlow;
                return true;

            default:
                sequenceIds = Array.Empty<string>();
                return false;
        }
    }

    public static bool RequiresCredits(RunEndReason reason)
    {
        return reason == RunEndReason.EndingTwo ||
            reason == RunEndReason.EndingThree;
    }
}
