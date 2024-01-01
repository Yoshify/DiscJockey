using UnityEngine;

namespace DiscJockey.Utils
{
    public class AssetUtils
    {
        public static AssetBundle DiscJockeyAssetBundle;

        public static GameObject NetworkManagerPrefab;
        public static GameObject TrackListButtonPrefab;
        public static GameObject UIManagerPrefab;

        public static void LoadAssetBundle(string assetBundlePath)
        {
            DiscJockeyPlugin.LogInfo($"AssetUtils<LoadAssetBundle>: Loading AssetBundle at {assetBundlePath}");
            DiscJockeyAssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            
            DiscJockeyPlugin.LogInfo($"AssetUtils<LoadAssetBundle>: Setting prefabs");
            UIManagerPrefab = DiscJockeyAssetBundle.LoadAsset<GameObject>("DJUIManager");
            TrackListButtonPrefab = DiscJockeyAssetBundle.LoadAsset<GameObject>("TrackListButton");
            NetworkManagerPrefab = DiscJockeyAssetBundle.LoadAsset<GameObject>("DJNetworkManager");
        }
    }
}