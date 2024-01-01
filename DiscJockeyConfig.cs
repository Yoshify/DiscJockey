using System;
using BepInEx;
using BepInEx.Configuration;
using DiscJockey.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiscJockey
{
    public class DiscJockeyConfig
    {
        public static ConfigEntry<string> DiscJockeyPanelHotkey;
        public static ConfigEntry<bool> ClearDownloadCacheAtReboot;
        public static ConfigEntry<bool> LoadDownloadedSongsFromCacheAtLaunch;
        public static ConfigEntry<int> MaxCachedDownloads;
        public static ConfigEntry<bool> DisableCreditsText;

        public static void Init()
        {
            DiscJockeyPanelHotkey = DiscJockeyPlugin.Instance.Config.Bind(
                "General",
                "Hotkey",
                "<Keyboard>/F10",
                "The key used to open DiscJockey in game. This is a Unity Input Action and specifically needs to follow the format of <Device>/Key."
            );
            
            DisableCreditsText = DiscJockeyPlugin.Instance.Config.Bind(
                "General",
                "Disable Credits Text",
                false,
                "On the bottom left of the panel is some credits - I couldn't have made this mod without the support of my friend group. If you'd prefer to hide this text, set this option to true."
            );
            
            LoadDownloadedSongsFromCacheAtLaunch = DiscJockeyPlugin.Instance.Config.Bind(
                "Downloads",
                "Load Downloaded Songs From Cache At Launch",
                false,
                "Downloaded songs are cached on disk to save bandwidth. By setting this to true, those songs will be reloaded after each session. Warning - this can cause tracklist desync as you may have songs on disk that other clients don't. This setting conflicts with 'Clear Download Cache At Reboot'"
            );

            ClearDownloadCacheAtReboot = DiscJockeyPlugin.Instance.Config.Bind(
                "Downloads",
                "Clear Download Cache At Launch",
                false,
                "Downloaded songs are cached on disk to save bandwidth. By setting this to true, this cache will be cleared each time you boot the game to save space."
            );
            
            MaxCachedDownloads = DiscJockeyPlugin.Instance.Config.Bind(
                "Downloads",
                "Maximum Cached Downloads",
                20,
                "Downloaded songs are cached on disk to save bandwidth. If a new song is added and the cache size is at maximum, the oldest item will be removed from the cache to make room."
            );
        }
    }
}