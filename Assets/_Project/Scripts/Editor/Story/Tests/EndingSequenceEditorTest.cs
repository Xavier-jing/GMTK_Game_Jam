using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EndingSequenceEditorTest
{
    private const string MenuPath =
        "Tools/Jam Template/Tests/Run Ending Sequence Unit Test";

    private static readonly Dictionary<string, string[]> ExpectedLines =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            {
                EndingSequenceId.Truth,
                new[]
                {
                    "I'm falling...",
                    "Everything that once puzzled me finally makes sense—like those strange symbols inside the refrigerator...",
                    "So I'm a doll...",
                    "Even so... I still want to live!",
                    "Path to other endings now Open."
                }
            },
            {
                EndingSequenceId.EndingOne,
                new[]
                {
                    "It hurts...",
                    "I don't want to die..."
                }
            },
            {
                EndingSequenceId.EndingTwo,
                new[]
                {
                    "My room used to be my entire world.",
                    "Whenever something didn't make sense, I never questioned it. I simply accepted it.",
                    "It wasn't until disaster found me that I finally saw the real world.",
                    "Then it's time to set off!",
                    "Ending Unlocked:Fly Away"
                }
            },
            {
                EndingSequenceId.EndingThree,
                new[]
                {
                    "The rope is tied tightly around me, and the wind howls in my ears.",
                    "I glance back at what was once my entire world.",
                    "All this time, I had been living inside an airplane.",
                    "Maybe it could still fly—but I've already made my choice.",
                    "I have no regrets.",
                    "Ending Unlocked:Safe Landing"
                }
            }
        };

    [MenuItem(MenuPath)]
    public static void Run()
    {
        RunAssertions();
        Debug.Log(
            "Ending sequence unit tests passed: catalog text, flow chaining, " +
            "terminal routing, and invalid data rejection were validated.");
    }

    public static void RunAssertions()
    {
        VerifyCatalog();
        VerifyFlowMappings();
        VerifyTerminalRouting();
        VerifyInvalidCatalogs();
    }

    private static void VerifyCatalog()
    {
        Assert(
            EndingSequenceCatalog.TryLoad(
                out EndingSequenceCatalog catalog,
                out string error),
            $"Expected ending sequence catalog to load: {error}");

        foreach (KeyValuePair<string, string[]> expected in ExpectedLines)
        {
            Assert(
                catalog.TryGet(expected.Key, out EndingSequenceData sequence),
                $"Expected catalog to contain '{expected.Key}'.");
            Assert(
                sequence.Lines.Length == expected.Value.Length,
                $"Expected '{expected.Key}' to contain " +
                $"{expected.Value.Length} lines, but found " +
                $"{sequence.Lines.Length}.");

            for (int index = 0; index < expected.Value.Length; index++)
            {
                Assert(
                    string.Equals(
                        sequence.Lines[index],
                        expected.Value[index],
                        StringComparison.Ordinal),
                    $"Unexpected text at '{expected.Key}' line {index}: " +
                    $"'{sequence.Lines[index]}'.");
            }
        }
    }

    private static void VerifyFlowMappings()
    {
        AssertFlow(
            RunEndReason.TruthRevealed,
            EndingSequenceId.Truth,
            EndingSequenceId.EndingOne);
        AssertFlow(
            RunEndReason.TurnsExhausted,
            EndingSequenceId.EndingOne);
        AssertFlow(
            RunEndReason.EndingOne,
            EndingSequenceId.EndingOne);
        AssertFlow(
            RunEndReason.EndingTwo,
            EndingSequenceId.EndingTwo);
        AssertFlow(
            RunEndReason.EndingThree,
            EndingSequenceId.EndingThree);

        Assert(
            !EndingSequenceFlow.RequiresCredits(
                RunEndReason.TruthRevealed),
            "TruthRevealed must not play credits.");
        Assert(
            !EndingSequenceFlow.RequiresCredits(
                RunEndReason.EndingOne),
            "EndingOne must not play credits.");
        Assert(
            EndingSequenceFlow.RequiresCredits(
                RunEndReason.EndingTwo),
            "EndingTwo must play credits.");
        Assert(
            EndingSequenceFlow.RequiresCredits(
                RunEndReason.EndingThree),
            "EndingThree must play credits.");
    }

    private static void VerifyTerminalRouting()
    {
        Assert(
            !LoopManager.IsTerminalEnding(RunEndReason.TruthRevealed),
            "TruthRevealed must start the next run.");
        Assert(
            !LoopManager.IsTerminalEnding(RunEndReason.EndingOne),
            "EndingOne must start the next run.");
        Assert(
            LoopManager.IsTerminalEnding(RunEndReason.EndingTwo),
            "EndingTwo must return to the main menu.");
        Assert(
            LoopManager.IsTerminalEnding(RunEndReason.EndingThree),
            "EndingThree must return to the main menu.");
    }

    private static void VerifyInvalidCatalogs()
    {
        const string duplicateJson =
            "{\"Version\":1,\"Sequences\":[" +
            "{\"Id\":\"EVT_TRUTH\",\"Lines\":[\"1\",\"2\",\"3\",\"4\",\"5\"]}," +
            "{\"Id\":\"EVT_TRUTH\",\"Lines\":[\"1\",\"2\",\"3\",\"4\",\"5\"]}]}";
        Assert(
            !EndingSequenceCatalog.TryParse(
                duplicateJson,
                out EndingSequenceCatalog _,
                out string duplicateError) &&
            duplicateError.Contains("duplicated"),
            "Expected duplicate ending sequence ids to be rejected.");

        const string wrongCountJson =
            "{\"Version\":1,\"Sequences\":[" +
            "{\"Id\":\"EVT_TRUTH\",\"Lines\":[\"only one\"]}]}";
        Assert(
            !EndingSequenceCatalog.TryParse(
                wrongCountJson,
                out EndingSequenceCatalog _,
                out string countError) &&
            countError.Contains("must contain 5"),
            "Expected invalid ending sequence line counts to be rejected.");
    }

    private static void AssertFlow(
        RunEndReason reason,
        params string[] expectedSequenceIds)
    {
        Assert(
            EndingSequenceFlow.TryGetSequenceIds(
                reason,
                out IReadOnlyList<string> actualSequenceIds),
            $"Expected a sequence flow for '{reason}'.");
        Assert(
            actualSequenceIds.Count == expectedSequenceIds.Length,
            $"Expected '{reason}' to contain {expectedSequenceIds.Length} " +
            $"sequence(s), but found {actualSequenceIds.Count}.");

        for (int index = 0; index < expectedSequenceIds.Length; index++)
        {
            Assert(
                string.Equals(
                    actualSequenceIds[index],
                    expectedSequenceIds[index],
                    StringComparison.Ordinal),
                $"Unexpected sequence at '{reason}' index {index}: " +
                $"'{actualSequenceIds[index]}'.");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
