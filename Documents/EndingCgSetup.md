# Ending CG placeholder setup

The run-ending flow waits for the ending presentation before starting the next
run. `LoopManager.EndRun` raises `RunEnded`; `GameplayUIController` displays the
configured placeholder and calls `LoopManager.CompleteRunEnding` when the
presentation finishes.

## GamePlay scene hookup

1. Open `Assets/_Project/Scenes/GamePlay.unity` in Unity `2022.3.62f3c1`.
2. Under `Canvas`, create an inactive full-screen UI object named `EndingCgRoot`.
3. Add an `Image` covering the full canvas:
   - anchors: stretch horizontally and vertically;
   - left, right, top, bottom: `0`;
   - color: opaque black `(0, 0, 0, 1)`;
   - Raycast Target: enabled.
4. Keep `GameplayUIController` on the always-active `Canvas` object. Do not put
   the controller on `EndingCgRoot`.
5. Assign `Canvas/EndingCgRoot` to `GameplayUIController.Ending Cg Root`.
6. Set `Ending Cg Placeholder Duration` to `2` seconds.
7. Enable `Auto Complete Ending Cg`.
8. Save the scene and review the Unity-generated serialized changes.

`EndingCgRoot` is activated after any ending reason. Player and UI input are
disabled while it is visible. The next Gameplay/Sandbox run starts only after
the placeholder completes.

## Replacing the placeholder later

Keep the same `EndingCgRoot` entry point. An animation or video completion event
can call `GameplayUIController.CompleteEndingCg()`. Disable
`Auto Complete Ending Cg` when the real presentation owns completion.
