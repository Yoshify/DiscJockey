using DiscJockey.Managers;
using DiscJockey.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiscJockey.Input;

public static class InputManager
{
    private static bool _hasInitialized;
    private static InputActionAsset _inputActions;
    private static InputActionMap _inputActionMap;
    private static InputAction _openDiscJockeyAction;

    public static string OpenDiscJockeyTooltip => $"Open DiscJockey:  [{GetOpenDiscJockeyBindingString()}]";

    public static void DisableInput(string inputName)
    {
        IngamePlayerSettings.Instance.playerInput.actions.FindAction(inputName).Disable();
    }

    public static void EnableInput(string inputName)
    {
        IngamePlayerSettings.Instance.playerInput.actions.FindAction(inputName).Enable();
    }

    public static void DisableMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void EnableMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public static void DisablePlayerLook()
    {
        LocalPlayerHelper.Player.disableLookInput = true;
        LocalPlayerHelper.Player.cursorIcon.enabled = true;
    }

    public static void EnablePlayerLook()
    {
        LocalPlayerHelper.Player.disableLookInput = false;
        LocalPlayerHelper.Player.cursorIcon.enabled = false;
    }

    public static void EnablePlayerInteractions()
    {
        EnableMouse();
        EnablePlayerLook();
        EnableInput("Look");
        EnableInput("Use");
        EnableInput("EnableChat");
        EnableInput("ActivateItem");
        EnableInput("SwitchItem");
        EnableInput("Discard");
        EnableInput("Crouch");
        EnableInput("OpenMenu");
    }

    public static void DisablePlayerInteractions()
    {
        DisableMouse();
        DisablePlayerLook();
        DisableInput("Look");
        DisableInput("Use");
        DisableInput("EnableChat");
        DisableInput("ActivateItem");
        DisableInput("SwitchItem");
        DisableInput("Discard");
        DisableInput("Crouch");
        DisableInput("OpenMenu");
    }

    private static void SetupKeybinds()
    {
        if (InputUtilsCompatibility.Enabled)
        {
            _inputActions = InputUtilsCompatibility.Asset;
            _inputActionMap = _inputActions.actionMaps[0];
            _openDiscJockeyAction = InputUtilsCompatibility.OpenDiscJockeyHotkey;
        }
        else
        {
            _inputActions = new InputActionAsset();
            _inputActionMap = new InputActionMap("DiscJockey");
            _inputActions.AddActionMap(_inputActionMap);
            _openDiscJockeyAction = new InputAction("DiscJockey.Open", InputActionType.Button, DiscJockeyConfig.LocalConfig.DiscJockeyPanelHotkey);
        }
        
        _openDiscJockeyAction.performed += ExecuteUIToggleAction;
        _openDiscJockeyAction.Enable();
        _inputActions.Enable();
    }
    
    public static string GetOpenDiscJockeyBindingString() => InputUtilsCompatibility.Enabled
        ? InputControlPath.ToHumanReadableString(InputUtilsCompatibility.OpenDiscJockeyHotkey.bindings[0].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice)
        : InputControlPath.ToHumanReadableString(DiscJockeyConfig.LocalConfig.DiscJockeyPanelHotkey, InputControlPath.HumanReadableStringOptions.OmitDevice);

    private static void ExecuteUIToggleAction(InputAction.CallbackContext ctx)
    {
        if (BoomboxManager.IsLookingAtOrHoldingBoombox) UIManager.Instance.ToggleUIPanel();
    }

    public static void Init()
    {
        
        if (_hasInitialized) return;
        SetupKeybinds();
        _hasInitialized = true;
    }
}