using DiscJockey.Data;

namespace DiscJockey.Managers
{
    public static class DiscJockeyBoomboxManager
    {
        public static BoomboxItem ActiveBoombox;
        public static bool InteractionsActive => ActiveBoombox != null;

        public static BoomboxMetadata ActiveBoomboxMetadata =>
            DiscJockeyNetworkManager.BoomboxNetworkMetadata[ActiveBoombox.NetworkObjectId];

        public static void EnableInteractionWithBoombox(BoomboxItem item)
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyBoomboxManager<EnableInteractionWithBoombox>: Enabling interaction");
            ActiveBoombox = item;
        }

        public static void DisableInteraction()
        {
            if (!InteractionsActive) return;

            DiscJockeyPlugin.LogInfo($"DiscJockeyBoomboxManager<DisableInteraction>: Disabling interaction");
            ActiveBoombox = null;
        }

    }
}
