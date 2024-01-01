using DiscJockey.Utils;
using GameNetcodeStuff;
using HarmonyLib;

namespace DiscJockey.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatches
    {
        [HarmonyPatch(typeof(PlayerControllerB), "Start")]
        [HarmonyPostfix]
        private static void StartPatch(PlayerControllerB __instance)
        {
            LocalPlayerHelper.TrySetLocalPlayer(__instance);
        }
    }
}
