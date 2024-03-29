﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Cysharp.Threading.Tasks;
using DiscJockey.Audio;
using DiscJockey.Input;
using DiscJockey.Utils;
using HarmonyLib;
using UnityEngine;
using UnityEngine.LowLevel;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace DiscJockey;

[BepInPlugin(PluginID, PluginName, PluginVersion)]
[BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.SoftDependency)]
public class DiscJockeyPlugin : BaseUnityPlugin
{
    public const string PluginID = "Yoshify.DiscJockey";
    public const string PluginName = "DiscJockey";
    public const string PluginVersion = "1.2.1";
    public const string PluginFolderName = "Yoshify-DiscJockey";
    public static string PluginFolderPath => Path.Combine(Paths.PluginPath, PluginFolderName);

    public static string AssetsPath => Path.Combine(PluginFolderPath, "Assets");
    
    public static string CustomSongsDirectory =>
        Path.Combine(PluginFolderPath, "Custom Songs");

    public static string DownloadCacheDirectory =>
        Path.Combine(PluginFolderPath, "Download Cache");

    public static string DownloadersDirectory =>
        Path.Combine(PluginFolderPath, "Downloaders");

    public static DiscJockeyPlugin Instance;
    private static bool _networkPatchInit;

    private readonly Harmony _harmony = new(PluginID);
    private ManualLogSource _logger;
    public new static DiscJockeyConfig Config { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;

        Config = new DiscJockeyConfig(base.Config);

        _logger = BepInEx.Logging.Logger.CreateLogSource(PluginID);
        
        LogInfo($"Init UniTask Loop");
        var loop = PlayerLoop.GetCurrentPlayerLoop();
        PlayerLoopHelper.Initialize(ref loop);
        
        LogInfo("Loading AssetBundle");
        AssetLoader.LoadAssetBundle(Path.Combine(AssetsPath,
            "discjockey"));

        LogInfo("Applying patches");
        _harmony.PatchAll();

        LogInfo("Initializing Network Patch");
        InitNetworkPatch();

        LogInfo("Initializing AudioLoader");
        AudioLoader.Init(DiscJockeyConfig.LocalConfig.MaxCachedDownloads, DownloadCacheDirectory,
            DownloadersDirectory);

        var uiManager = Instantiate(AssetLoader.UIManagerPrefab);
        DontDestroyOnLoad(uiManager);
        uiManager.hideFlags = HideFlags.HideAndDontSave;

        InputManager.Init();
        AudioManager.Init();

        LogInfo("DiscJockey initialized");
    }

    private static void InitNetworkPatch()
    {
        if (_networkPatchInit) return;

        IEnumerable<Type> types;
        try
        {
            types = Assembly.GetExecutingAssembly().GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            types = e.Types.Where(t => t != null);
        }
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length > 0) method.Invoke(null, null);
            }
        }

        _networkPatchInit = true;
    }


    internal static void LogDebug(string message, [CallerMemberName] string methodName = "", [CallerFilePath] string filePath = "")
    {
        Instance.Log($"[{Path.GetFileNameWithoutExtension(filePath)}.{methodName}]: {message}", LogLevel.Debug);
    }

    internal static void LogInfo(string message, [CallerMemberName] string methodName = "", [CallerFilePath] string filePath = "")
    {
        Instance.Log($"[{Path.GetFileNameWithoutExtension(filePath)}.{methodName}]: {message}", LogLevel.Info);
    }

    internal static void LogWarning(string message, [CallerMemberName] string methodName = "", [CallerFilePath] string filePath = "")
    {
        Instance.Log($"[{Path.GetFileNameWithoutExtension(filePath)}.{methodName}]: {message}", LogLevel.Warning);
    }

    internal static void LogError(string message, [CallerMemberName] string methodName = "", [CallerFilePath] string filePath = "")
    {
        Instance.Log($"[{Path.GetFileNameWithoutExtension(filePath)}.{methodName}]: {message}", LogLevel.Error);
    }

    private void Log(string message, LogLevel logLevel)
    {
        _logger.Log(logLevel, $"[{PluginName} {PluginVersion}] - [{DateTime.Now:HH:mm:ss.ff}] - {message}");
    }
}