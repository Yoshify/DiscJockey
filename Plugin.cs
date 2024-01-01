using BepInEx;
using BepInEx.Logging;
using DiscJockey.Patches;
using DiscJockey.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using System.Reflection;
using DiscJockey.API;
using UnityEngine.SceneManagement;
using DiscJockey.Utils;
using UnityEngine.InputSystem;

namespace DiscJockey
{
    [BepInPlugin(ModID, ModName, ModVersion)]
    public class DiscJockeyPlugin: BaseUnityPlugin
    {
        public const string ModID = "Yoshify.DiscJockey";
        public const string ModName = "DiscJockey";
        public const string ModVersion = "1.0.0";
        
        public static readonly string CustomSongsDirectory = Path.Combine(Paths.PluginPath, "Yoshify-DiscJockey", "Custom Songs");
        public static readonly string DownloadedAudioDirectory = Path.Combine(Paths.PluginPath, "Yoshify-DiscJockey", "Download Cache");
        public static readonly string DownloadersDirectory = Path.Combine(Paths.PluginPath, "Yoshify-DiscJockey", "Downloaders");

        private readonly Harmony _harmony = new Harmony(ModID);
        public static DiscJockeyPlugin Instance;
        private ManualLogSource _logger;
        private static bool _networkPatchInit;

        void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
            }
            
            DiscJockeyConfig.Init();

            _logger = BepInEx.Logging.Logger.CreateLogSource(ModID);
            LogInfo($"DiscJockeyPlugin<Awake>: Loading AssetBundle");
            AssetUtils.LoadAssetBundle(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "discjockey"));

            LogInfo($"DiscJockeyPlugin<Awake>: Applying patches");
            _harmony.PatchAll(typeof(DiscJockeyPlugin));
            _harmony.PatchAll(typeof(GrabbableObjectPatches));
            _harmony.PatchAll(typeof(PlayerControllerBPatches));
            _harmony.PatchAll(typeof(StartOfRoundPatches));
            _harmony.PatchAll(typeof(BoomboxItemPatches));
            _harmony.PatchAll(typeof(GameNetworkManagerPatches));

            LogInfo($"DiscJockeyPlugin<Awake>: Initializing Network Patch");
            InitNetworkPatch();

            LogInfo($"DiscJockeyPlugin<Awake>: Initializing AudioLoaderAPI");
            AudioLoaderAPI.Init(DiscJockeyConfig.MaxCachedDownloads.Value, DownloadedAudioDirectory, DownloadersDirectory);
            
            var uiManager = Instantiate(AssetUtils.UIManagerPrefab);
            DontDestroyOnLoad(uiManager);
            uiManager.hideFlags = HideFlags.HideAndDontSave;
            LogInfo($"DiscJockeyPlugin<Awake>: Spawned DiscJockeyUIManager: {uiManager != null}");
            
            DiscJockeyInputManager.Init();
            DiscJockeyAudioManager.Init();

            LogInfo($"DiscJockeyPlugin<Awake>: DiscJockey initialized");
        }

        private static void InitNetworkPatch()
        {
            if (_networkPatchInit)
            {
                return;
            } 
            
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }

            _networkPatchInit = true;
        }


        internal static void LogDebug(string message) => Instance.Log(message, LogLevel.Debug);
        internal static void LogInfo(string message) => Instance.Log(message, LogLevel.Info);
        internal static void LogWarning(string message) => Instance.Log(message, LogLevel.Warning);
        internal static void LogError(string message) => Instance.Log(message, LogLevel.Error);

        private void Log(string message, LogLevel logLevel) => _logger.Log(logLevel, $"{ModName} {ModVersion}: {message}");
    }
}
