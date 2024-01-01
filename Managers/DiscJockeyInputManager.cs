using DiscJockey.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiscJockey.Managers
{
    public static class DiscJockeyInputManager
    {
        private static bool _hasInitialized = false;
        public static void DisableInput(string inputName) => IngamePlayerSettings.Instance.playerInput.actions.FindAction(inputName).Disable();
        public static void EnableInput(string inputName) => IngamePlayerSettings.Instance.playerInput.actions.FindAction(inputName).Enable();

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
            DisableInput("ActivateItem");
            DisableInput("SwitchItem");
            DisableInput("Discard");
            DisableInput("Crouch");
            DisableInput("OpenMenu");
        }

        public static void Create()
        {
            
        }

        private static void SetupInputAction()
        {
            var toggleDiscJockeyUIAction = new InputAction(binding: DiscJockeyConfig.DiscJockeyPanelHotkey.Value);
            toggleDiscJockeyUIAction.performed += (context) =>
            {
                if (DiscJockeyBoomboxManager.InteractionsActive)
                {
                    DiscJockeyUIManager.Instance.ToggleUIPanel();
                }
            };
            toggleDiscJockeyUIAction.Enable();
        }

        public static void Init()
        {
            if (_hasInitialized) return;
            SetupInputAction();
            _hasInitialized = true;
        }
    }
}
