using UnityEngine;

namespace DiscJockey;

public class ConfigValidator
{
    public static float ValidateInterfaceTransparency()
    {
        var configuredTransparency = DiscJockeyConfig.LocalConfig.InterfaceTransparency;
        if (configuredTransparency is < 0.1f or > 1.0f)
        {
            DiscJockeyPlugin.LogWarning($"Failed to set interface transparency from config - {configuredTransparency} is not within the accepted range of 0.1 to 1.0!");
            return 0.45f;
        }

        return configuredTransparency;
    }

    public static Color ValidateInterfaceColour()
    {
        var configuredColour = DiscJockeyConfig.LocalConfig.InterfaceColour;
        if (!configuredColour.StartsWith('#')) configuredColour = $"#{configuredColour}";
        if (ColorUtility.TryParseHtmlString(configuredColour, out var colour)) return colour;

        DiscJockeyPlugin.LogWarning($"Failed to set interface colour from config - {configuredColour} is not a valid hex colour code!");
        ColorUtility.TryParseHtmlString("#0077FF", out colour);
        return colour;
    }
}