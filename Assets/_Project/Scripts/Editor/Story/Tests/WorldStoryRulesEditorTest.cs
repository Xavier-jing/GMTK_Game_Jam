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
            "visual states, carry mappings, and new story handlers are valid.");
    }

    public static void RunAssertions()
    {
        VerifyProgressRules();
        VerifyVisualStateRules();
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
        Assert(
            !WorldPropRules.IsPresent(
                WorldPropId.Wrench,
                loopProgress,
                runState),
            "The wrench must remain hidden before the dresser opens.");

        runState.MarkDresserOpened();
        Assert(
            WorldPropRules.IsPresent(
                WorldPropId.Scissors,
                loopProgress,
                runState),
            "Opening the dresser must reveal the scissors.");
        Assert(
            WorldPropRules.IsPresent(
                WorldPropId.Wrench,
                loopProgress,
                runState),
            "Opening the dresser must reveal the wrench.");

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
            WorldPropRules.IsPresent(
                WorldPropId.Plank,
                loopProgress,
                runState),
            "The plank must be available before the truth event.");
        Assert(
            !WorldPropRules.IsPresent(
                WorldPropId.Rope,
                loopProgress,
                runState),
            "The bed rope must not appear as a standalone world prop.");

        runState.MarkRopeTaken();
        Assert(
            WorldPropRules.IsPresent(
                WorldPropId.Rope,
                loopProgress,
                runState),
            "Taking the bed rope must reveal its interaction target.");

        loopProgress.RevealTruth();
        Assert(
            WorldPropRules.IsPresent(
                WorldPropId.BedBlanket,
                loopProgress,
                runState),
            "The blanket interaction must appear after the truth is revealed.");
        Assert(
            !WorldPropRules.IsPresent(
                WorldPropId.BedSwitch,
                loopProgress,
                runState),
            "The bed switch must stay hidden until the bed is lifted.");

        runState.MarkBedLifted();
        Assert(
            WorldPropRules.IsPresent(
                WorldPropId.BedSwitch,
                loopProgress,
                runState),
            "Lifting the bed must reveal its switch.");

        runState.MarkBedSwitchTriggered();
        Assert(
            WorldPropRules.IsPresent(
                WorldPropId.SteeringWheel,
                loopProgress,
                runState),
            "Pressing the bed switch must reveal the steering wheel.");

        runState.MarkSteeringWheelRaised();
        Assert(
            WorldPropRules.IsPresent(
                WorldPropId.PowerConnector,
                loopProgress,
                runState),
            "Starting the steering wheel must reveal the bed power connector.");

        runState.Reset();
        Assert(
            !runState.DresserOpened &&
            !runState.FabricPrepared &&
            !runState.BedLifted &&
            !runState.RopeTaken &&
            !runState.WallStruckOnce,
            "RunState.Reset must clear per-run prop progress.");
        Assert(
            loopProgress.TruthKnown,
            "RunState.Reset must not clear truth knowledge.");
    }

    private static void VerifyVisualStateRules()
    {
        RunState runState = new RunState();

        Assert(
            WorldPropRules.HasChangedVisual(WorldPropId.Dresser),
            "The dresser must support closed and open sprites.");
        Assert(
            !WorldPropRules.UsesChangedVisual(WorldPropId.Dresser, runState),
            "The dresser must start with its closed sprite.");

        runState.MarkDresserOpened();
        Assert(
            WorldPropRules.UsesChangedVisual(WorldPropId.Dresser, runState),
            "Opening the dresser must select its open sprite.");

        Assert(
            WorldPropRules.HasChangedVisual(WorldPropId.CableBed),
            "The cable bed must support closed and lifted sprites.");
        Assert(
            !WorldPropRules.UsesChangedVisual(WorldPropId.CableBed, runState),
            "The cable bed must start with its closed sprite.");

        runState.MarkBedLifted();
        Assert(
            WorldPropRules.UsesChangedVisual(WorldPropId.CableBed, runState),
            "Lifting the cable bed must select its open sprite.");

        runState.MarkFridgeUnplugged();
        Assert(
            !WorldPropRules.HasChangedVisual(WorldPropId.Refrigerator) &&
            !WorldPropRules.UsesChangedVisual(
                WorldPropId.Refrigerator,
                runState),
            "Unplugging the refrigerator must not be treated as opening its door.");

        runState.Reset();
        Assert(
            !WorldPropRules.UsesChangedVisual(WorldPropId.Dresser, runState) &&
            !WorldPropRules.UsesChangedVisual(WorldPropId.CableBed, runState),
            "RunState.Reset must restore default prop visuals.");
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
