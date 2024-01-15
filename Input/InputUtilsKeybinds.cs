using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;


namespace DiscJockey.Input;

public class InputUtilsKeybinds : LcInputActions
{
    public static InputUtilsKeybinds Instance = new InputUtilsKeybinds();
    
    [InputAction("<Keyboard>/F10", Name = "[DiscJockey]\nOpen DiscJockey")]
    public InputAction OpenDiscJockeyHotkey { get; set; }

    public static InputActionAsset GetAsset() => Instance.Asset;
}