using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private const string InputActionsResourcePath = "Input/InputActions";

    private InputActionAsset actions;
    private InputActionMap gameplayMap;

    private InputAction movementAction;
    private InputAction jumpAction;
    private InputAction interactAction;
    // private InputAction inventoryAction;

    public Vector2 RawMovementInput { get; private set; }
    public int NormInputX { get; private set; }
    public int NormInputY { get; private set; }
    public bool JumpInput { get; private set; }

    [SerializeField]
    private float inputHoldTime = 0.2f;

    // [SerializeField]
    // private GameObject inventory;

    private float jumpInputStartTime;

    public static event Action<PlayerInputHandler> OnInteractPressed;

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
            Debug.LogError($"PlayerInputHandler: Input actions not found at Resources/{InputActionsResourcePath}.inputactions");
            return;
        }

        actions = Instantiate(template);
        actions.name = $"{template.name} (Runtime)";

        gameplayMap = actions.FindActionMap("GamePlay", true);
        if (gameplayMap == null)
        {
            Debug.LogError("PlayerInputHandler: 'GamePlay' action map not found in input actions.");
            return;
        }

        movementAction = gameplayMap.FindAction("Movement", true);
        jumpAction = gameplayMap.FindAction("Jump", true);
        interactAction = gameplayMap.FindAction("Interact", true);
        //inventoryAction = gameplayMap.FindAction("Inventory", true);

        if (movementAction != null)
        {
            movementAction.performed += OnMovementPerformed;
            movementAction.canceled += OnMovementCanceled;
        }

        if (jumpAction != null)
        {
            jumpAction.started += OnJumpStarted;
        }

        if (interactAction != null)
        {
            interactAction.performed += OnInteractPerformed;
        }

        // if (inventoryAction != null)
        // {
        //     inventoryAction.started += OnInventoryStarted;
        // }

        // GamePlay map enabled by default
        gameplayMap.Enable();

        IsInitialized = true;
    }

    private void Update()
    {
        CheckJumpInputHoldTime();
    }

    #region Input Callbacks

    private void OnMovementPerformed(InputAction.CallbackContext ctx)
    {
        RawMovementInput = ctx.ReadValue<Vector2>();

        NormInputX = Mathf.RoundToInt(RawMovementInput.x);
        NormInputY = Mathf.RoundToInt(RawMovementInput.y);
    }

    private void OnMovementCanceled(InputAction.CallbackContext ctx)
    {
        RawMovementInput = Vector2.zero;
        NormInputX = 0;
        NormInputY = 0;
    }

    private void OnJumpStarted(InputAction.CallbackContext ctx)
    {
        JumpInput = true;
        jumpInputStartTime = Time.time;
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        OnInteractPressed?.Invoke(this);
    }

    // private void OnInventoryStarted(InputAction.CallbackContext ctx)
    // {
    //     if (inventory != null)
    //     {
    //         inventory.SetActive(!inventory.activeSelf);
    //     }
    // }

    #endregion

    #region Public API

    public void UseJumpInput() => JumpInput = false;

    public void EnableGameplayInput()
    {
        if (IsInitialized && gameplayMap != null)
        {
            gameplayMap.Enable();
        }
    }

    public void DisableGameplayInput()
    {
        if (IsInitialized && gameplayMap != null)
        {
            gameplayMap.Disable();
        }
    }

    #endregion

    private void CheckJumpInputHoldTime()
    {
        if (JumpInput && Time.time >= jumpInputStartTime + inputHoldTime)
        {
            JumpInput = false;
        }
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
