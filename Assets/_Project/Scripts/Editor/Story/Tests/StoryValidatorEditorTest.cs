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
            "StoryValidator unit tests passed: graphs, dialogue options, audio actions, " +
            "turn changes, portrait ids, CG ids, and CG actions were validated successfully.");
    }

    public static void RunAssertions()
    {
        VerifyBasicStoryGraph();
        VerifyRandomDialogueOptions();
        VerifyPortraitIds();
        VerifyPortraitIdMappings();
        VerifyCgIds();
        VerifyCgActions();
        VerifyInitialDialoguePortraitPaths();
        VerifyAudioActions();
        VerifyTurnChangeAction();
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

    private static void VerifyRandomDialogueOptions()
    {
        StoryDocumentData document = new StoryDocumentData
        {
            Version = StoryValidator.SupportedVersion,
            ScriptId = "RandomDialogueStory",
            StartNodeId = "random",
            Nodes = new[]
            {
                new StoryNodeData
                {
                    Id = "random",
                    Type = nameof(StoryNodeType.Dialogue),
                    DialogOptions = new[]
                    {
                        "Option A",
                        "Option B",
                        "Option C"
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
        Assert(
            validator.TryValidate(
                document,
                out StoryGraph graph,
                out List<string> errors),
            $"Expected DialogOptions to validate: {string.Join(" | ", errors)}");
        Assert(
            graph != null && graph.ContainsNode("random"),
            "Expected the random dialogue node to compile.");
    }

    private static void VerifyPortraitIds()
    {
        StoryDocumentData validDocument = new StoryDocumentData
        {
            Version = StoryValidator.SupportedVersion,
            ScriptId = "PortraitStory",
            StartNodeId = "intro",
            Nodes = new[]
            {
                new StoryNodeData
                {
                    Id = "intro",
                    Type = nameof(StoryNodeType.Dialogue),
                    PortraitId = "face01",
                    Dialog = "Portrait test.",
                    Next = "finish"
                },
                new StoryNodeData
                {
                    Id = "finish",
                    Type = nameof(StoryNodeType.End)
                }
            }
        };

        StoryValidator validator = new StoryValidator(
            new StoryActionRegistry(),
            new StoryConditionRegistry());
        Assert(
            validator.TryValidate(
                validDocument,
                out StoryGraph validGraph,
                out List<string> validErrors),
            $"Expected a valid PortraitId to compile: {string.Join(" | ", validErrors)}");
        Assert(
            validGraph != null && validGraph.ContainsNode("intro"),
            "Expected the graph containing a valid PortraitId to be returned.");

        validDocument.Nodes[0].PortraitId = "face 01";
        Assert(
            !validator.TryValidate(
                validDocument,
                out StoryGraph _,
                out List<string> invalidIdErrors),
            "Expected a PortraitId containing a space to fail validation.");
        Assert(
            invalidIdErrors.Exists(error => error.Contains("PortraitId")),
            "Expected the invalid PortraitId error to identify PortraitId.");

        StoryDocumentData nonDialogueDocument = new StoryDocumentData
        {
            Version = StoryValidator.SupportedVersion,
            ScriptId = "NonDialoguePortraitStory",
            StartNodeId = "finish",
            Nodes = new[]
            {
                new StoryNodeData
                {
                    Id = "finish",
                    Type = nameof(StoryNodeType.End),
                    PortraitId = "face01"
                }
            }
        };
        Assert(
            !validator.TryValidate(
                nonDialogueDocument,
                out StoryGraph _,
                out List<string> nonDialogueErrors),
            "Expected PortraitId on a non-Dialogue node to fail validation.");
        Assert(
            nonDialogueErrors.Exists(
                error => error.Contains("only valid on Dialogue")),
            "Expected the non-Dialogue PortraitId error to explain its allowed node type.");
    }

    private static void VerifyPortraitIdMappings()
    {
        Assert(
            StoryPortraitIdMap.ResolveBindingId("0") == "face01",
            "Expected fail-safe PortraitId 0 to resolve to face01.");
        Assert(
            StoryPortraitIdMap.ResolveBindingId("1") == "face01",
            "Expected PortraitId 1 to resolve to face01.");
        Assert(
            StoryPortraitIdMap.ResolveBindingId("2") == "face02",
            "Expected PortraitId 2 to resolve to face02.");
        Assert(
            StoryPortraitIdMap.ResolveBindingId("3") == "face03",
            "Expected PortraitId 3 to resolve to face03.");
        Assert(
            StoryPortraitIdMap.ResolveBindingId("4") == "face04",
            "Expected PortraitId 4 to resolve to face04.");
        Assert(
            StoryPortraitIdMap.ResolveBindingId("5") == "face05",
            "Expected PortraitId 5 to resolve to face05.");
        Assert(
            StoryPortraitIdMap.ResolveBindingId("face03") == "face03",
            "Expected existing face03 ids to remain backward compatible.");
    }

    private static void VerifyCgIds()
    {
        StoryDocumentData validDocument = new StoryDocumentData
        {
            Version = StoryValidator.SupportedVersion,
            ScriptId = "CgStory",
            StartNodeId = "intro",
            Nodes = new[]
            {
                new StoryNodeData
                {
                    Id = "intro",
                    Type = nameof(StoryNodeType.Dialogue),
                    CgId = "1",
                    Dialog = "CG test.",
                    Next = "finish"
                },
                new StoryNodeData
                {
                    Id = "finish",
                    Type = nameof(StoryNodeType.End)
                }
            }
        };

        StoryValidator validator = new StoryValidator(
            new StoryActionRegistry(),
            new StoryConditionRegistry());
        Assert(
            validator.TryValidate(
                validDocument,
                out StoryGraph validGraph,
                out List<string> validErrors),
            $"Expected a valid CgId to compile: {string.Join(" | ", validErrors)}");
        Assert(
            validGraph != null && validGraph.ContainsNode("intro"),
            "Expected the graph containing a valid CgId to be returned.");
        Assert(
            StoryCgId.TryParse("0", out int hideNumber) &&
            hideNumber == StoryCgId.Hide,
            "Expected CgId 0 to resolve to the hide command.");
        Assert(
            StoryCgId.TryParse("2147483647", out int maximumNumber) &&
            maximumNumber == int.MaxValue,
            "Expected CgId Int32.MaxValue to be valid.");

        validDocument.Nodes[0].CgId = "0";
        Assert(
            validator.TryValidate(
                validDocument,
                out StoryGraph _,
                out List<string> hideErrors),
            $"Expected CgId 0 to compile: {string.Join(" | ", hideErrors)}");

        string[] invalidIds =
        {
            "-1",
            "01",
            "cg01",
            "2147483648"
        };
        foreach (string invalidId in invalidIds)
        {
            validDocument.Nodes[0].CgId = invalidId;
            Assert(
                !validator.TryValidate(
                    validDocument,
                    out StoryGraph _,
                    out List<string> invalidErrors),
                $"Expected invalid CgId '{invalidId}' to fail validation.");
            Assert(
                invalidErrors.Exists(error => error.Contains("CgId")),
                $"Expected the invalid CgId error to identify '{invalidId}'.");
        }

        StoryDocumentData nonDialogueDocument = new StoryDocumentData
        {
            Version = StoryValidator.SupportedVersion,
            ScriptId = "NonDialogueCgStory",
            StartNodeId = "finish",
            Nodes = new[]
            {
                new StoryNodeData
                {
                    Id = "finish",
                    Type = nameof(StoryNodeType.End),
                    CgId = "1"
                }
            }
        };
        Assert(
            !validator.TryValidate(
                nonDialogueDocument,
                out StoryGraph _,
                out List<string> nonDialogueErrors),
            "Expected CgId on a non-Dialogue node to fail validation.");
        Assert(
            nonDialogueErrors.Exists(
                error => error.Contains("CgId") &&
                         error.Contains("only valid on Dialogue")),
            "Expected the non-Dialogue CgId error to explain its allowed node type.");
    }

    private static void VerifyCgActions()
    {
        StoryActionRegistry registry = new StoryActionRegistry();
        Assert(
            registry.TryGet(
                HideCgStoryAction.ActionId,
                out IStoryActionHandler hideCg),
            "Expected HideCg to be registered.");
        Assert(
            hideCg.Validate(null, out string validationError),
            $"Expected HideCg to require no Params: {validationError}");

        StoryDocumentData document = new StoryDocumentData
        {
            Version = StoryValidator.SupportedVersion,
            ScriptId = "HideCgActionStory",
            StartNodeId = "line",
            Nodes = new[]
            {
                new StoryNodeData
                {
                    Id = "line",
                    Type = nameof(StoryNodeType.Dialogue),
                    CgId = "1",
                    Dialog = "Hide the CG after this line.",
                    AfterActions = new[]
                    {
                        new StoryActionData
                        {
                            Id = HideCgStoryAction.ActionId
                        }
                    },
                    Next = "finish"
                },
                new StoryNodeData
                {
                    Id = "finish",
                    Type = nameof(StoryNodeType.End)
                }
            }
        };

        StoryValidator validator = new StoryValidator(
            registry,
            new StoryConditionRegistry());
        Assert(
            validator.TryValidate(
                document,
                out StoryGraph graph,
                out List<string> errors),
            $"Expected HideCg in AfterActions to compile: {string.Join(" | ", errors)}");
        Assert(
            graph != null && graph.ContainsNode("line"),
            "Expected the graph containing HideCg to be returned.");
    }

    private static void VerifyInitialDialoguePortraitPaths()
    {
        StoryDocumentData branchingDocument = new StoryDocumentData
        {
            Version = StoryValidator.SupportedVersion,
            ScriptId = "BranchingPortraitStory",
            StartNodeId = "menu",
            Nodes = new[]
            {
                new StoryNodeData
                {
                    Id = "menu",
                    Type = nameof(StoryNodeType.Choice),
                    Choices = new[]
                    {
                        new StoryChoiceData
                        {
                            Dialog = "Configured",
                            TargetScriptId = "BranchingPortraitStory",
                            TargetNodeId = "configured"
                        },
                        new StoryChoiceData
                        {
                            Dialog = "Missing",
                            TargetScriptId = "BranchingPortraitStory",
                            TargetNodeId = "missing"
                        }
                    }
                },
                new StoryNodeData
                {
                    Id = "configured",
                    Type = nameof(StoryNodeType.Dialogue),
                    PortraitId = "face01",
                    Dialog = "Configured portrait.",
                    Next = "finish"
                },
                new StoryNodeData
                {
                    Id = "missing",
                    Type = nameof(StoryNodeType.Dialogue),
                    Dialog = "Missing portrait.",
                    Next = "finish"
                },
                new StoryNodeData
                {
                    Id = "finish",
                    Type = nameof(StoryNodeType.End)
                }
            }
        };

        StoryGraph branchingGraph = CompileStory(branchingDocument);
        Dictionary<string, StoryGraph> branchingGraphs =
            new Dictionary<string, StoryGraph>(StringComparer.Ordinal)
            {
                { branchingDocument.ScriptId, branchingGraph }
            };
        Dictionary<string, string> branchingPaths =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { branchingDocument.ScriptId, "branching.json" }
            };
        List<string> branchingErrors = new List<string>();
        StoryScriptValidationMenu.ValidateInitialDialoguePortraits(
            branchingGraphs,
            branchingPaths,
            branchingErrors);
        Assert(
            branchingErrors.Count == 1 &&
            branchingErrors[0].Contains("BranchingPortraitStory/missing"),
            "Expected the branch whose first Dialogue lacks PortraitId to be reported.");

        StoryDocumentData inheritedDocument = new StoryDocumentData
        {
            Version = StoryValidator.SupportedVersion,
            ScriptId = "InheritedPortraitStory",
            StartNodeId = "first",
            Nodes = new[]
            {
                new StoryNodeData
                {
                    Id = "first",
                    Type = nameof(StoryNodeType.Dialogue),
                    PortraitId = "face03",
                    Dialog = "Set portrait.",
                    Next = "second"
                },
                new StoryNodeData
                {
                    Id = "second",
                    Type = nameof(StoryNodeType.Dialogue),
                    Dialog = "Keep portrait.",
                    Next = "finish"
                },
                new StoryNodeData
                {
                    Id = "finish",
                    Type = nameof(StoryNodeType.End)
                }
            }
        };
        Dictionary<string, StoryGraph> inheritedGraphs =
            new Dictionary<string, StoryGraph>(StringComparer.Ordinal)
            {
                { inheritedDocument.ScriptId, CompileStory(inheritedDocument) }
            };
        Dictionary<string, string> inheritedPaths =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { inheritedDocument.ScriptId, "inherited.json" }
            };
        List<string> inheritedErrors = new List<string>();
        StoryScriptValidationMenu.ValidateInitialDialoguePortraits(
            inheritedGraphs,
            inheritedPaths,
            inheritedErrors);
        Assert(
            inheritedErrors.Count == 0,
            "Expected later Dialogue nodes to inherit the first valid portrait.");

        StoryDocumentData rootDocument = new StoryDocumentData
        {
            Version = StoryValidator.SupportedVersion,
            ScriptId = "CrossScriptRoot",
            StartNodeId = "menu",
            Nodes = new[]
            {
                new StoryNodeData
                {
                    Id = "menu",
                    Type = nameof(StoryNodeType.Choice),
                    Choices = new[]
                    {
                        new StoryChoiceData
                        {
                            Dialog = "Cross script",
                            TargetScriptId = "CrossScriptTarget",
                            TargetNodeId = "entry"
                        }
                    }
                }
            }
        };
        StoryDocumentData targetDocument = new StoryDocumentData
        {
            Version = StoryValidator.SupportedVersion,
            ScriptId = "CrossScriptTarget",
            StartNodeId = "finish",
            Nodes = new[]
            {
                new StoryNodeData
                {
                    Id = "entry",
                    Type = nameof(StoryNodeType.Dialogue),
                    Dialog = "Cross-script portrait is missing.",
                    Next = "finish"
                },
                new StoryNodeData
                {
                    Id = "finish",
                    Type = nameof(StoryNodeType.End)
                }
            }
        };
        Dictionary<string, StoryGraph> crossScriptGraphs =
            new Dictionary<string, StoryGraph>(StringComparer.Ordinal)
            {
                { rootDocument.ScriptId, CompileStory(rootDocument) },
                { targetDocument.ScriptId, CompileStory(targetDocument) }
            };
        Dictionary<string, string> crossScriptPaths =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { rootDocument.ScriptId, "root.json" },
                { targetDocument.ScriptId, "target.json" }
            };
        List<string> crossScriptErrors = new List<string>();
        StoryScriptValidationMenu.ValidateInitialDialoguePortraits(
            crossScriptGraphs,
            crossScriptPaths,
            crossScriptErrors);
        Assert(
            crossScriptErrors.Count == 1 &&
            crossScriptErrors[0].Contains("CrossScriptTarget/entry"),
            "Expected a cross-script first Dialogue without PortraitId to be reported.");
    }

    private static void VerifyTurnChangeAction()
    {
        StoryActionRegistry registry = new StoryActionRegistry();
        Assert(
            registry.TryGet("ChangeTurns", out IStoryActionHandler changeTurns),
            "Expected ChangeTurns to be registered.");

        Assert(
            changeTurns.Validate(
                new StoryActionParams { IntValue = int.MinValue },
                out string minimumError),
            $"Expected Int32.MinValue to be a valid turn delta: {minimumError}");
        Assert(
            changeTurns.Validate(
                new StoryActionParams { IntValue = 0 },
                out string zeroError),
            $"Expected zero to be a valid no-op turn delta: {zeroError}");
        Assert(
            changeTurns.Validate(
                new StoryActionParams { IntValue = int.MaxValue },
                out string maximumError),
            $"Expected Int32.MaxValue to be a valid turn delta: {maximumError}");

        TurnManager turnManager = new TurnManager(initialTurns: 5);
        turnManager.ChangeTurns(int.MaxValue);
        Assert(
            turnManager.RemainingTurns == int.MaxValue,
            "Expected a positive overflow to clamp remaining turns to Int32.MaxValue.");

        turnManager.ResetTurns();
        turnManager.ChangeTurns(int.MinValue);
        Assert(
            turnManager.RemainingTurns == 0,
            "Expected a negative overflow to clamp remaining turns to zero.");
    }

    private static StoryGraph CompileStory(StoryDocumentData document)
    {
        StoryValidator validator = new StoryValidator(
            new StoryActionRegistry(),
            new StoryConditionRegistry());
        bool succeeded = validator.TryValidate(
            document,
            out StoryGraph graph,
            out List<string> errors);
        Assert(
            succeeded,
            $"Expected test story '{document.ScriptId}' to compile: " +
            string.Join(" | ", errors));
        return graph;
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
