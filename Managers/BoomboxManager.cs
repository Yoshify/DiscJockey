﻿using DiscJockey.Input;
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

    public static bool LookedAtOrHeldBoomboxIsNot(ulong networkedBoomboxId)
    {
        if (!IsLookingAtOrHoldingBoombox) return false;
        return LookedAtOrHeldBoombox.NetworkedBoomboxId != networkedBoomboxId;
    }

    public static void OnHeldBoombox(ulong boomboxNetworkId)
    {
        DiscJockeyPlugin.LogInfo($"DiscJockeyBoomboxManager<OnHeldBoombox>: Now holding Boombox {boomboxNetworkId}");
        if (DJNetworkManager.Boomboxes.TryGetValue(boomboxNetworkId, out var networkedBoombox))
            HeldBoombox = networkedBoombox;
    }

    public static void OnDroppedBoombox()
    {
        if (!IsHoldingBoombox) return;

        DiscJockeyPlugin.LogInfo($"DiscJockeyBoomboxManager<OnDroppedBoombox>: Dropped {HeldBoombox.NetworkedBoomboxId}");
        HeldBoombox = null;
    }

    public static void OnLookedAtBoombox(BoomboxItem boomboxItem)
    {
        if (DJNetworkManager.Boomboxes.TryGetValue(boomboxItem.NetworkObjectId, out var networkedBoombox))
            LookedAtBoombox = networkedBoombox;
    }

    public static void OnLookedAwayFromBoombox()
    {
        if (!IsLookingAtBoombox) return;
        
        LookedAtBoombox = null;
    }
}