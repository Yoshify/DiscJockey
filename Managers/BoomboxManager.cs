using DiscJockey.Input;
using DiscJockey.Networking;

namespace DiscJockey.Managers;

public static class BoomboxManager
{
    private const string OriginalBoomboxGrabTip = "Grab boombox:  [E]";
    public static NetworkedBoombox HeldBoombox;
    public static NetworkedBoombox LookedAtBoombox;

    public static NetworkedBoombox LookedAtOrHeldBoombox =>
        IsLookingAtBoombox ? LookedAtBoombox : IsHoldingBoombox ? HeldBoombox : null;

    public static bool IsLookingAtOrHoldingBoombox => LookedAtOrHeldBoombox != null;
    public static bool IsLookingAtBoombox => LookedAtBoombox != null;
    public static bool IsHoldingBoombox => HeldBoombox != null;

    public static bool HeldBoomboxIsNot(ulong networkedBoomboxId)
    {
        if (!IsLookingAtOrHoldingBoombox)
        {
            return false;
        }

        return LookedAtOrHeldBoombox.NetworkedBoomboxId != networkedBoomboxId;
    }

    public static void EnableInteractionWithBoombox(ulong boomboxNetworkId)
    {
        DiscJockeyPlugin.LogInfo("DiscJockeyBoomboxManager<EnableInteractionWithBoombox>: Enabling interaction");
        if (DJNetworkManager.Boomboxes.TryGetValue(boomboxNetworkId, out var networkedBoombox))
            HeldBoombox = networkedBoombox;
    }

    public static void DisableInteraction()
    {
        if (!IsLookingAtOrHoldingBoombox) return;

        DiscJockeyPlugin.LogInfo("DiscJockeyBoomboxManager<DisableInteraction>: Disabling interaction");
        HeldBoombox = null;
    }

    public static void OnLookedAtBoombox(BoomboxItem boomboxItem)
    {

        boomboxItem.customGrabTooltip =
            $"{OriginalBoomboxGrabTip}\n{InputManager.OpenDiscJockeyTooltip}";
        if (DJNetworkManager.Boomboxes.TryGetValue(boomboxItem.NetworkObjectId, out var networkedBoombox))
            LookedAtBoombox = networkedBoombox;
    }

    public static void OnLookedAwayFromBoombox()
    {
        LookedAtBoombox.Boombox.customGrabTooltip = OriginalBoomboxGrabTip;
        LookedAtBoombox = null;
    }
}