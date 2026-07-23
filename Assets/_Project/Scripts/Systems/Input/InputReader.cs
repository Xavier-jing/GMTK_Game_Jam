using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InputReader : IDisposable
{
    private readonly InputActionMap gameplayMap;

    public InputAction Move { get; }

    public InputAction Pause { get; }

    public InputAction Submit { get; }

    public InputAction Cancel { get; }

    public bool IsEnabled => gameplayMap.enabled;

    public Vector2 MoveValue => Move.ReadValue<Vector2>();

    public bool PausePressedThisFrame => Pause.WasPressedThisFrame();

    public bool SubmitPressedThisFrame => Submit.WasPressedThisFrame();

    public bool CancelPressedThisFrame => Cancel.WasPressedThisFrame();

    public InputReader()
    {
        gameplayMap = new InputActionMap("Gameplay");

        Move = gameplayMap.AddAction("Move", InputActionType.Value);
        AddKeyboardMoveBindings(Move, "w", "s", "a", "d");
        AddKeyboardMoveBindings(Move, "upArrow", "downArrow", "leftArrow", "rightArrow");
        Move.AddBinding("<Gamepad>/leftStick");

        Pause = gameplayMap.AddAction("Pause", InputActionType.Button);
        Pause.AddBinding("<Keyboard>/escape");
        Pause.AddBinding("<Gamepad>/start");

        Submit = gameplayMap.AddAction("Submit", InputActionType.Button);
        Submit.AddBinding("<Keyboard>/enter");
        Submit.AddBinding("<Keyboard>/space");
        Submit.AddBinding("<Gamepad>/buttonSouth");

        Cancel = gameplayMap.AddAction("Cancel", InputActionType.Button);
        Cancel.AddBinding("<Keyboard>/escape");
        Cancel.AddBinding("<Gamepad>/buttonEast");

        Enable();
    }

    public void Enable()
    {
        gameplayMap.Enable();
    }

    public void Disable()
    {
        gameplayMap.Disable();
    }

    public void Dispose()
    {
        gameplayMap.Disable();
        gameplayMap.Dispose();
    }

    private static void AddKeyboardMoveBindings(
        InputAction action,
        string up,
        string down,
        string left,
        string right)
    {
        action
            .AddCompositeBinding("2DVector")
            .With("Up", $"<Keyboard>/{up}")
            .With("Down", $"<Keyboard>/{down}")
            .With("Left", $"<Keyboard>/{left}")
            .With("Right", $"<Keyboard>/{right}");
    }
}
