using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class WorldStoryRulesEditorTest
{
    private const string MenuPath =
        "Tools/Jam Template/Tests/Run World Story Rules Unit Test";

    [MenuItem(MenuPath)]
    public static void Run()
    {
        RunAssertions();
        Debug.Log(
            "World story rules unit test passed: prop visibility, loop reset, " +
            "carry mappings, and new story handlers are valid.");
    }

    public static void RunAssertions()
    {
        VerifyProgressRules();
        VerifyCarryMappings();
        VerifyStoryContract();
    }

    private static void VerifyProgressRules()
    {
        LoopProgress loopProgress = new LoopProgress();
        RunState runState = new RunState();

        Assert(
            WorldPropRules.IsPresent(
                WorldPropId.Dresser,
                loopProgress,
                runState),
            "The dresser must be present at the start of a run.");
        Assert(
            !WorldPropRules.IsPresent(
                WorldPropId.Scissors,
                loopProgress,
                runState),
            "The scissors must remain hidden before the dresser opens.");

        runState.MarkDresserOpened();
        Assert(
            WorldPropRules.IsPresent(
                WorldPropId.Scissors,
                loopProgress,
                runState),
            "Opening the dresser must reveal the scissors.");

        runState.MarkWallStruckOnce();
        Assert(
            WorldPropRules.IsPresent(
                WorldPropId.LargeWallHole,
                loopProgress,
                runState),
            "The large wall hole must replace the small hole after the first strike.");
        Assert(
            !WorldPropRules.IsPresent(
                WorldPropId.SmallWallHole,
                loopProgress,
                runState),
            "The small wall hole must be hidden before the truth is revealed.");
        Assert(
            !WorldPropRules.IsPresent(
                WorldPropId.Plank,
                loopProgress,
                runState),
            "The plank must remain locked until the truth is known.");

        loopProgress.RevealTruth();
        Assert(
            WorldPropRules.IsPresent(
                WorldPropId.Plank,
                loopProgress,
                runState),
            "The plank must appear after the truth is revealed.");
        Assert(
            WorldPropRules.IsPresent(
                WorldPropId.BedBlanket,
                loopProgress,
                runState),
            "The blanket interaction must appear after the truth is revealed.");

        runState.Reset();
        Assert(
            !runState.DresserOpened &&
            !runState.FabricPrepared &&
            !runState.WallStruckOnce,
            "RunState.Reset must clear per-run prop progress.");
        Assert(
            loopProgress.TruthKnown,
            "RunState.Reset must not clear truth knowledge.");
    }

    private static void VerifyCarryMappings()
    {
        Assert(
            WorldPropRules.GetSlotItemKind(WorldPropId.Dresser) ==
            PlayerSlotItemKind.Dresser,
            "The dresser must map to its weighted slot kind.");
        Assert(
            WorldPropRules.GetSlotItemKind(WorldPropId.Refrigerator) ==
            PlayerSlotItemKind.Refrigerator,
            "The refrigerator must map to its weighted slot kind.");
        Assert(
            WorldPropRules.GetSlotItemKind(WorldPropId.Wrench) ==
            PlayerSlotItemKind.None,
            "Inventory items must not enter the weighted carry slot.");
    }

    private static void VerifyStoryContract()
    {
        StoryDocumentData document = new StoryDocumentData
        {
            Version = StoryValidator.SupportedVersion,
            ScriptId = "WorldPropUnitTest",
            StartNodeId = "choice",
            Nodes = new[]
            {
                new StoryNodeData
                {
                    Id = "choice",
                    Type = nameof(StoryNodeType.Choice),
                    Choices = new[]
                    {
                        new StoryChoiceData
                        {
                            Dialog = "Take",
                            TargetScriptId = "WorldPropUnitTest",
                            TargetNodeId = "execute",
                            Condition = new StoryConditionData
                            {
                                Id = "WorldPropCommandAvailable",
                                UnavailableMode =
                                    nameof(StoryUnavailableMode.Disabled),
                                Params = new StoryActionParams
                                {
                                    TargetId = "TeaSetTarget",
                                    StringValue =
                                        nameof(WorldPropCommand.TakeIntoCarrySlot)
                                }
                            }
                        }
                    }
                },
                new StoryNodeData
                {
                    Id = "execute",
                    Type = nameof(StoryNodeType.Action),
                    Actions = new[]
                    {
                        new StoryActionData
                        {
                            Id = "WorldPropCommand",
                            Params = new StoryActionParams
                            {
                                TargetId = "TeaSetTarget",
                                StringValue =
                                    nameof(WorldPropCommand.TakeIntoCarrySlot)
                            }
                        },
                        new StoryActionData
                        {
                            Id = "SpendTurns",
                            Params = new StoryActionParams
                            {
                                IntValue = 1
                            }
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
            new StoryActionRegistry(),
            new StoryConditionRegistry());
        bool succeeded = validator.TryValidate(
            document,
            out StoryGraph graph,
            out List<string> errors);

        Assert(
            succeeded,
            $"New world story handlers must validate: {string.Join(" | ", errors)}");
        Assert(graph != null, "The world prop test story must compile to a graph.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                $"World story rules unit test failed: {message}");
        }
    }
}
