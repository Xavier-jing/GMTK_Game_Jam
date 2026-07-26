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
            "StoryValidator unit tests passed: the basic graph and story audio actions " +
            "were validated and compiled successfully.");
    }

    public static void RunAssertions()
    {
        VerifyBasicStoryGraph();
        VerifyAudioActions();
    }

    private static void VerifyBasicStoryGraph()
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

    private static void VerifyAudioActions()
    {
        StoryActionRegistry registry = new StoryActionRegistry();
        Assert(
            registry.TryGet(
                PlaySfxStoryAction.ActionId,
                out IStoryActionHandler playSfx),
            "Expected PlaySfx to be registered.");
        Assert(
            registry.TryGet(
                SwitchBgmStoryAction.ActionId,
                out IStoryActionHandler switchBgm),
            "Expected SwitchBgm to be registered.");

        StoryActionParams validSfx = new StoryActionParams
        {
            StringValue = "door_open",
            FloatValue = 0.8f
        };
        Assert(
            playSfx.Validate(validSfx, out string validSfxError),
            $"Expected valid PlaySfx parameters: {validSfxError}");

        StoryActionParams defaultSfxVolume = new StoryActionParams
        {
            StringValue = "door_open",
            FloatValue = 0f
        };
        Assert(
            playSfx.Validate(defaultSfxVolume, out string defaultSfxError),
            $"Expected zero to select the default SFX volume: {defaultSfxError}");

        StoryActionParams invalidSfxId = new StoryActionParams
        {
            StringValue = "door/open",
            FloatValue = 1f
        };
        Assert(
            !playSfx.Validate(invalidSfxId, out string _),
            "Expected PlaySfx to reject an audio id containing a path separator.");

        StoryActionParams invalidSfxVolume = new StoryActionParams
        {
            StringValue = "door_open",
            FloatValue = 1.1f
        };
        Assert(
            !playSfx.Validate(invalidSfxVolume, out string _),
            "Expected PlaySfx to reject volume values above 1.");

        StoryActionParams validBgm = new StoryActionParams
        {
            StringValue = "gameplay",
            FloatValue = 1.5f
        };
        Assert(
            switchBgm.Validate(validBgm, out string validBgmError),
            $"Expected valid SwitchBgm parameters: {validBgmError}");

        StoryActionParams invalidBgmFade = new StoryActionParams
        {
            StringValue = "gameplay",
            FloatValue = -0.1f
        };
        Assert(
            !switchBgm.Validate(invalidBgmFade, out string _),
            "Expected SwitchBgm to reject a negative fade duration.");

        StoryDocumentData audioDocument = new StoryDocumentData
        {
            Version = StoryValidator.SupportedVersion,
            ScriptId = "AudioActionStory",
            StartNodeId = "audio",
            Nodes = new[]
            {
                new StoryNodeData
                {
                    Id = "audio",
                    Type = nameof(StoryNodeType.Action),
                    Actions = new[]
                    {
                        new StoryActionData
                        {
                            Id = PlaySfxStoryAction.ActionId,
                            Params = validSfx
                        },
                        new StoryActionData
                        {
                            Id = SwitchBgmStoryAction.ActionId,
                            Params = validBgm
                        }
                    },
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
            registry,
            new StoryConditionRegistry());
        Assert(
            validator.TryValidate(
                audioDocument,
                out StoryGraph audioGraph,
                out List<string> audioErrors),
            $"Expected audio actions to compile: {string.Join(" | ", audioErrors)}");
        Assert(
            audioGraph != null && audioGraph.ContainsNode("audio"),
            "Expected the compiled audio story to contain its action node.");
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
