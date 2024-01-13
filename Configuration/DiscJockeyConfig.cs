using System;
using System.Runtime.Serialization;
using BepInEx.Configuration;
using Unity.Collections;
using Unity.Netcode;

namespace DiscJockey;

[DataContract]
public class DiscJockeyConfig : NetworkedConfig<DiscJockeyConfig>
{
    [DataMember] public bool ClearDownloadCacheAtReboot;

    [DataMember] public float DefaultVolume;

    [DataMember] public bool DisableBatteryDrain;

    [DataMember] public bool DisableCreditsText;

    [DataMember] public string DiscJockeyPanelHotkey;

    [DataMember] public string InterfaceColour;

    [DataMember] public float InterfaceTransparency;

    [DataMember] public bool LoadDownloadedSongsFromCacheAtLaunch;

    [DataMember] public int MaxCachedDownloads;

    [DataMember] public bool NetworkedVolumeControl;

    [DataMember] public bool AddVanillaSongsToTracklist;
    
    [DataMember] public bool EntitiesHearMusic;

    [DataMember] public bool KeepDownloadedSongsPermanently;

    public DiscJockeyConfig(ConfigFile cfg)
    {
        InitInstance(this);

        DiscJockeyPanelHotkey = cfg.Bind(
            "Gameplay",
            "Hotkey",
            "<Keyboard>/F10",
            "[NOTE: If using InputUtils, this bind will be ignored. Please refer to changing the keybind in game with InputUtils instead] The key used to open DiscJockey in game. This is a Unity Input Action and specifically needs to follow the format of <Device>/Key."
        ).Value;
        
        AddVanillaSongsToTracklist = cfg.Bind(
            "Gameplay",
            "Add Vanilla Boombox Music To Tracklist",
            false,
            "If set to true, the vanilla Boombox music will be added to your tracklist."
        ).Value;
        
        EntitiesHearMusic = cfg.Bind(
            "Gameplay",
            "Enemies Hear Music",
            true,
            "[OVERRIDDEN BY HOST] If set to false, the Boombox will become inaudible to enemies (e.g, the blind dog, the slime)"
        ).Value;

        InterfaceColour = cfg.Bind(
            "Interface",
            "Interface Colour",
            "#0077FF",
            "Changes the colour of the DiscJockey interface. Hex colour codes only."
        ).Value;

        InterfaceTransparency = cfg.Bind(
            "Interface",
            "Interface Transparency",
            0.5f,
            "Changes how transparent the DiscJockey interface is. Accepted values are in the range of 0.1 to 1.0"
        ).Value;

        DisableBatteryDrain = cfg.Bind(
            "Gameplay",
            "Disable Battery Drain",
            false,
            "[OVERRIDDEN BY HOST] If set to true, Boomboxes will no longer drain battery."
        ).Value;

        NetworkedVolumeControl = cfg.Bind(
            "Volume",
            "Networked Volume Control",
            false,
            "[OVERRIDDEN BY HOST] If set to true, DiscJockey will keep the volume of Boomboxes in sync over the network."
        ).Value;

        DefaultVolume = cfg.Bind(
            "Volume",
            "Default Boombox Volume",
            0.8f,
            "The default volume of DiscJockey controlled Boomboxes. Accepted range is 0.0 to 1.0. Note if 'Networked Volume Control' is enabled by your host, this will likely be overriden."
        ).Value;

        DisableCreditsText = cfg.Bind(
            "Interface",
            "Disable Credits Text",
            false,
            "On the bottom left of the panel is some credits - I couldn't have made this mod without the support of my friend group. If you'd prefer to hide this text, set this option to true."
        ).Value;
        
        KeepDownloadedSongsPermanently = cfg.Bind(
            "Downloads",
            "Permanently Keep Downloaded Songs",
            false,
            "Instead of being cached, downloaded songs will instead be saved to your Custom Songs folder to keep around permanently."
        ).Value;

        LoadDownloadedSongsFromCacheAtLaunch = cfg.Bind(
            "Downloads",
            "Load Downloaded Songs From Cache At Launch",
            false,
            "Downloaded songs are cached on disk to save bandwidth. By setting this to true, those songs will be reloaded after each session. Warning - this can cause tracklist desync as you may have songs on disk that other clients don't. This setting conflicts with 'Clear Download Cache At Reboot'"
        ).Value;

        ClearDownloadCacheAtReboot = cfg.Bind(
            "Downloads",
            "Clear Download Cache At Launch",
            false,
            "Downloaded songs are cached on disk to save bandwidth. By setting this to true, this cache will be cleared each time you boot the game to save space."
        ).Value;

        MaxCachedDownloads = cfg.Bind(
            "Downloads",
            "Maximum Cached Downloads",
            20,
            "Downloaded songs are cached on disk to save bandwidth. If a new song is added and the cache size is at maximum, the oldest item will be removed from the cache to make room."
        ).Value;
    }

    public static void RequestSync()
    {
        if (!IsClient) return;

        using FastBufferWriter stream = new(IntSize, Allocator.Temp);
        SendMessage("DiscJockey_OnRequestConfigSync", 0uL, stream);
    }

    public static void OnRequestSync(ulong clientId, FastBufferReader _)
    {
        if (!IsHost) return;

        DiscJockeyPlugin.LogInfo(
            $"DiscJockeyConfig<OnRequestSync>: Config sync request received from client: {clientId}");

        var array = SerializeToBytes(SyncedConfig);
        var value = array.Length;

        using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

        try
        {
            stream.WriteValueSafe(in value);
            stream.WriteBytesSafe(array);

            SendMessage("DiscJockey_OnReceiveConfigSync", clientId, stream);
        }
        catch (Exception e)
        {
            DiscJockeyPlugin.LogWarning(
                $"DiscJockeyConfig<OnRequestSync>: Error occurred syncing config with client: {clientId}\n{e}");
        }
    }

    public static void OnReceiveSync(ulong _, FastBufferReader reader)
    {
        if (!reader.TryBeginRead(IntSize))
        {
            DiscJockeyPlugin.LogError("DiscJockeyConfig<OnReceiveSync>: Could not begin reading buffer");
            return;
        }

        reader.ReadValueSafe(out int val);
        if (!reader.TryBeginRead(val))
        {
            DiscJockeyPlugin.LogError("DiscJockeyConfig<OnReceiveSync>: Host could not sync");
            return;
        }

        var data = new byte[val];
        reader.ReadBytesSafe(ref data, val);

        SyncInstance(data);

        DiscJockeyPlugin.LogInfo("DiscJockeyConfig<OnReceiveSync>: Successfully synced config with host");
    }
}