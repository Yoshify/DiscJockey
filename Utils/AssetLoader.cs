using UnityEngine;

namespace DiscJockey.Utils;

public static class AssetLoader
{
    public static AssetBundle DiscJockeyAssetBundle { get; private set; }
    public static GameObject NetworkManagerPrefab { get; private set; }
    public static GameObject TrackListButtonPrefab { get; private set; }
    public static GameObject UIManagerPrefab { get; private set; }

    public static void LoadAssetBundle(string assetBundlePath)
    {
        DiscJockeyPlugin.LogInfo($"AssetUtils<LoadAssetBundle>: Loading AssetBundle at {assetBundlePath}");
        DiscJockeyAssetBundle = AssetBundle.LoadFromFile(assetBundlePath);

        DiscJockeyPlugin.LogInfo("AssetUtils<LoadAssetBundle>: Setting prefabs");
        UIManagerPrefab = DiscJockeyAssetBundle.LoadAsset<GameObject>("DiscJockeyUIManager");
        TrackListButtonPrefab = DiscJockeyAssetBundle.LoadAsset<GameObject>("TrackListButton");
        NetworkManagerPrefab = DiscJockeyAssetBundle.LoadAsset<GameObject>("DiscJockeyNetworkManager");
    }
}