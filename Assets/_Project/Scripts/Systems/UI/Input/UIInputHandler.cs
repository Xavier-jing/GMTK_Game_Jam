using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class UIInputHandler : MonoBehaviour
{
    private const string InputActionsResourcePath = "Input/InputActions";

    private InputActionAsset actions;
    private InputActionMap uiMap;

    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;
    private InputAction pointAction;
    private InputAction clickAction;
    private InputAction scrollWheelAction;
    private InputAction pauseAction;

    public event Action OnPause;
    public event Action OnSubmit;
    public event Action OnCancel;

    public Vector2 NavigateValue { get; private set; }
    public Vector2 PointValue { get; private set; }
    public Vector2 ScrollWheelValue { get; private set; }

    public bool SubmitPressedThisFrame => submitAction != null && submitAction.WasPressedThisFrame();
    public bool CancelPressedThisFrame => cancelAction != null && cancelAction.WasPressedThisFrame();
    public bool PausePressedThisFrame => pauseAction != null && pauseAction.WasPressedThisFrame();

    public bool IsUINavigationEnabled { get; private set; }
    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (IsInitialized)
        {
            return;
        }

        InputActionAsset template = Resources.Load<InputActionAsset>(InputActionsResourcePath);
        if (template == null)
        {
            Debug.LogError($"UIInputHandler: Input actions not found at Resources/{InputActionsResourcePath}.inputactions");
            return;
        }

        actions = Instantiate(template);
        actions.name = $"{template.name} (Runtime)";

        uiMap = actions.FindActionMap("UI", true);
        if (uiMap == null)
        {
            Debug.LogError("UIInputHandler: 'UI' action map not found in input actions.");
            return;
        }

        navigateAction = uiMap.FindAction("Navigate", true);
        submitAction = uiMap.FindAction("Submit", true);
        cancelAction = uiMap.FindAction("Cancel", true);
        pointAction = uiMap.FindAction("Point", true);
        clickAction = uiMap.FindAction("Click", true);
        scrollWheelAction = uiMap.FindAction("ScrollWheel", true);
        pauseAction = uiMap.FindAction("Pause", true);

        // Button events
        if (submitAction != null) submitAction.performed += _ => OnSubmit?.Invoke();
        if (cancelAction != null) cancelAction.performed += _ => OnCancel?.Invoke();
        if (pauseAction != null) pauseAction.performed += _ => OnPause?.Invoke();

        // Continuous value updates
        if (navigateAction != null)
        {
            navigateAction.performed += ctx => NavigateValue = ctx.ReadValue<Vector2>();
            navigateAction.canceled += _ => NavigateValue = Vector2.zero;
        }

        if (pointAction != null)
        {
            pointAction.performed += ctx => PointValue = ctx.ReadValue<Vector2>();
            pointAction.canceled += _ => PointValue = Vector2.zero;
        }

        if (scrollWheelAction != null)
        {
            scrollWheelAction.performed += ctx => ScrollWheelValue = ctx.ReadValue<Vector2>();
            scrollWheelAction.canceled += _ => ScrollWheelValue = Vector2.zero;
        }

        // UI map always enabled
        uiMap.Enable();

        IsInitialized = true;
    }

    public void EnableUINavigation()
    {
        IsUINavigationEnabled = true;
    }

    public void DisableUINavigation()
    {
        IsUINavigationEnabled = false;
    }

    public void Enable()
    {
        if (!IsInitialized)
        {
            return;
        }

        uiMap.Enable();
    }

    public void Disable()
    {
        if (!IsInitialized)
        {
            return;
        }

        actions.Disable();
    }

    private void OnDestroy()
    {
        if (actions != null)
        {
            actions.Disable();
            Destroy(actions);
            actions = null;
        }
    }
}
