# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `/Volumes/Workstation/Unity/GMTK_Game_Jam`
- Last analyzed: 2026-07-26
- Last analyzed commit: `3387625a5c458c695c39d0d4e79a90684b9595dd`
- Scope: short-cycle 2D Game Jam project with a JSON-driven story system.

## Confirmed Environment

- Unity version: `2022.3.62f3c1` (`1623fc0bbb97`)
- Render pipeline: Universal Render Pipeline 14.0.12
- Input system: Unity Input System 1.14.2
- Target platforms: not confirmed from the inspected sources

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | Unity 2D feature set and URP | Confirmed | `Packages/manifest.json` |
| Input | Unity Input System | Confirmed | `Packages/manifest.json`, `ProjectSettings/EditorBuildSettings.asset` |
| Camera | Cinemachine 2.10.7 | Confirmed | `Packages/manifest.json` |
| UI | uGUI and TextMesh Pro | Confirmed | `Packages/manifest.json`, `Assets/_Project/Scripts/Systems/UI/` |
| Story | Versioned JSON graphs loaded from `Resources/Story` | Confirmed | `Documents/StoryInterpreter.md`, `Assets/_Project/Scripts/Systems/Story/` |
| Unity MCP | Funplay Unity MCP package declared | Confirmed | `Packages/manifest.json` |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/_Project/Scripts/Core/` | App lifetime, loops, scenes, turns | Confirmed | Representative C# files |
| `Assets/_Project/Scripts/Game/` | Player and world interaction runtime | Confirmed | Representative C# files |
| `Assets/_Project/Scripts/Systems/Story/` | Story loader, validator, runner and handlers | Confirmed | Source files |
| `Assets/_Project/Scripts/Editor/Story/Tests/` | Menu-driven editor assertions | Confirmed | Test source files |
| `Assets/_Project/Resources/Story/` | Runtime story JSON | Confirmed | JSON resources |

## Assembly Boundaries

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| `Assembly-CSharp` | First-party runtime code | UnityEngine, project packages | No first-party runtime `.asmdef` found |
| `Assembly-CSharp-Editor` | Editor validation and tests | UnityEditor, runtime assembly | No first-party editor `.asmdef` found |

## Scenes And Startup Flow

- Build scenes: `Boot`, `MainMenu`, `GamePlay`
- Likely startup scene: `Assets/_Project/Scenes/Boot.unity`
- Scene loading flow: `AppContext` owns `SceneLoader`; run endings reload `GamePlay` or `Sandbox` when active.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| App services | Persistent `AppContext` creates and exposes core services | Confirmed | `Assets/_Project/Scripts/Core/App/AppContext.cs` |
| Gameplay | MonoBehaviour components with plain C# loop, inventory and turn state | Confirmed | `Assets/_Project/Scripts/Game/`, `Assets/_Project/Scripts/Core/` |
| Story | Data-driven graph with registered action and condition handlers | Confirmed | `Assets/_Project/Scripts/Systems/Story/` |
| Interaction | `IInteractable` implementations start story scripts; semantic commands recheck prerequisites | Confirmed | `WorldStoryInteractable.cs`, `WorldStoryHandlers.cs` |

## Coding Conventions

- Namespace style: project-first scripts currently use the global namespace.
- Serialized fields: private fields with `[SerializeField]`, usually one attribute and declaration per line.
- Async: story actions use `Task` and `CancellationToken`; most gameplay logic is synchronous.
- Comments/docs: comments explain lifecycle or gameplay intent; detailed authoring rules live in `Documents/`.

## Testing And Validation

- EditMode tests: menu-driven assertion classes under `Assets/_Project/Scripts/Editor/Story/Tests/`.
- PlayMode tests: none found in the inspected first-party folders.
- CI/build validation: no project CI configuration found.

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Unity MCP package | available in project | `Packages/manifest.json` |
| Unity MCP live connection | unavailable | local MCP endpoint was not reachable on 2026-07-26 |
| Repository inspection | available | attached Codex workspace |
| Unity editor compilation and Console readback | unavailable for this analysis | no live MCP connection |

## Important Constraints

- `AGENTS.md` allows automated writes only to `.cs`, `.json`, and `.md`.
- AI-assisted art/audio generation or editing is prohibited.
- Scene, prefab, serialized asset, package, and Project Settings writes require human handling.
- New scripts must be imported by Unity and receive Unity-generated `.meta` files.

## Unknowns And Confidence

- Story and world-interaction components were not found serialized in the inspected scenes or prefabs, so current Inspector wiring is unverified.
- Target platforms and release build commands remain unknown.
- Runtime story behavior requires human Play Mode validation until the Unity MCP connection is available.

## Source Files Inspected

- `AGENTS.md`
- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`
- `Documents/StoryInterpreter.md`
- Representative files under `Assets/_Project/Scripts/Core/`, `Game/Interaction/`, and `Systems/Story/`
- Story JSON under `Assets/_Project/Resources/Story/`

<!-- unity-onboarding:generated:end -->
