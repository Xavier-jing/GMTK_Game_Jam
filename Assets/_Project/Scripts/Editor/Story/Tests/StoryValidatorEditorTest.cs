using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class StoryValidatorEditorTest
{
    private const string MenuPath =
        "Tools/Jam Template/Tests/Run Story Validator Unit Test";

    [MenuItem(MenuPath)]
    public static void Run()
    {
        RunAssertions();

        Debug.Log(
            "StoryValidator unit test passed: a Dialogue node followed by an End node " +
            "was validated and compiled successfully.");
    }

    public static void RunAssertions()
    {
        StoryDocumentData document = new StoryDocumentData
        {
            Version = StoryValidator.SupportedVersion,
            ScriptId = "UnitTestStory",
            StartNodeId = "intro",
            Nodes = new[]
            {
                new StoryNodeData
                {
                    Id = "intro",
                    Type = nameof(StoryNodeType.Dialogue),
                    ActorId = "Narrator",
                    Dialog = "The story begins.",
                    Next = "finish"
                },
                new StoryNodeData
                {
                    Id = "finish",
                    Type = nameof(StoryNodeType.End),
                    Result = "Passed"
                }
            }
        };

        StoryValidator validator = new StoryValidator(
            new StoryActionRegistry(),
            new StoryConditionRegistry());

        bool succeeded = validator.TryValidate(
            document,
            out StoryGraph graph,
            out List<string> errors);

        Assert(
            succeeded,
            $"Expected a valid linear story, but validation failed: {string.Join(" | ", errors)}");
        Assert(graph != null, "Expected validation to return a compiled story graph.");
        Assert(
            graph.ContainsNode("intro"),
            "Expected the compiled graph to contain the dialogue node.");
        Assert(
            graph.ContainsNode("finish"),
            "Expected the compiled graph to contain the end node.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                $"StoryValidator unit test failed: {message}");
        }
    }
}
