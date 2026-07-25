using UnityEngine;

/// <summary>
/// Temporary Play Mode panel for testing turns and run endings.
/// Attach this component to any active GameObject in Gameplay or Sandbox.
/// Remove it when the real interactions are connected.
/// </summary>
[DisallowMultipleComponent]
public sealed class TurnSystemTest : MonoBehaviour
{
    [SerializeField]
    private bool showPanel = true;

    [SerializeField]
    private Rect panelRect = new Rect(20f, 80f, 240f, 270f);

    private AppContext appContext;

    private void Start()
    {
        appContext = AppContext.Instance;
    }

    private void OnGUI()
    {
        if (!showPanel || !Application.isPlaying)
        {
            return;
        }

        if (appContext == null)
        {
            appContext = AppContext.Instance;
        }

        panelRect = GUILayout.Window(
            GetInstanceID(),
            panelRect,
            DrawPanel,
            "Turn System Test");
    }

    private void DrawPanel(int windowId)
    {
        TurnManager turns = appContext.TurnManager;
        LoopProgress progress = appContext.LoopProgress;

        GUILayout.Label($"Loop: {progress.CurrentLoop}");
        GUILayout.Label($"Turns: {turns.RemainingTurns}");
        GUILayout.Label($"Truth known: {progress.TruthKnown}");

        GUI.enabled = !appContext.LoopManager.IsEndingRun;

        if (GUILayout.Button("Normal action  (-1)"))
        {
            ResolveNormalAction(-1, "Normal action");
        }

        if (GUILayout.Button("Repair action  (+2)"))
        {
            ResolveNormalAction(+2, "Repair action");
        }

        if (GUILayout.Button("Exhaust turns  (Ending 1)"))
        {
            ResolveEndingOne();
        }

        if (GUILayout.Button("Reveal truth immediately"))
        {
            ResolveEnding(RunEndReason.TruthRevealed);
        }

        GUI.enabled =
            !appContext.LoopManager.IsEndingRun &&
            progress.TruthKnown;

        if (GUILayout.Button("Reach ending 2"))
        {
            ResolveEnding(RunEndReason.EndingTwo);
        }

        if (GUILayout.Button("Reach ending 3"))
        {
            ResolveEnding(RunEndReason.EndingThree);
        }

        GUI.enabled = true;
        GUI.DragWindow();
    }

    private void ResolveNormalAction(int turnDelta, string actionName)
    {
        appContext.ActionResolver.Resolve(
            turnDelta,
            () => Debug.Log(
                $"[TurnSystemTest] Executed {actionName}, delta={turnDelta}.",
                this));
    }

    private void ResolveEnding(RunEndReason reason)
    {
        appContext.ActionResolver.Resolve(
            turnDelta: 0,
            execute: () => Debug.Log(
                $"[TurnSystemTest] Requested run ending: {reason}.",
                this),
            immediateEndReason: reason);
    }

    private void ResolveEndingOne()
    {
        int remainingTurns = appContext.TurnManager.RemainingTurns;

        appContext.ActionResolver.Resolve(
            turnDelta: -remainingTurns,
            execute: () => Debug.Log(
                "[TurnSystemTest] Exhausted all remaining turns.",
                this));
    }
}
