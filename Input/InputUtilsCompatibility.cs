using UnityEngine.InputSystem;

namespace DiscJockey.Input;

public static class InputUtilsCompatibility
{
    public static InputActionAsset Asset => InputUtilsKeybinds.GetAsset();
    public static bool Enabled =>
        BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.LethalCompanyInputUtils");

    public static InputAction OpenDiscJockeyHotkey => InputUtilsKeybinds.Instance.OpenDiscJockeyHotkey;
}