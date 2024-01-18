using System;
using System.IO;
using System.Runtime.Serialization;
using Unity.Netcode;

namespace DiscJockey;

[Serializable]
public class NetworkedConfig<T>
{
    [NonSerialized] protected static int IntSize = 4;
    [NonSerialized] private static readonly DataContractSerializer serializer = new(typeof(T));

    internal static bool Synced;
    public static CustomMessagingManager MessageManager => NetworkManager.Singleton.CustomMessagingManager;
    public static bool IsClient => NetworkManager.Singleton.IsClient;
    public static bool IsHost => NetworkManager.Singleton.IsHost;

    internal static T LocalConfig { get; private set; }
    internal static T SyncedConfig { get; private set; }

    protected void InitInstance(T instance)
    {
        LocalConfig = instance;
        SyncedConfig = instance;

        IntSize = sizeof(int);
    }

    internal static void SyncInstance(byte[] data)
    {
        SyncedConfig = DeserializeFromBytes(data);
        Synced = true;
    }

    internal static void RevertSync()
    {
        SyncedConfig = LocalConfig;
        Synced = false;
    }

    public static byte[] SerializeToBytes(T val)
    {
        using MemoryStream stream = new();

        try
        {
            serializer.WriteObject(stream, val);
            return stream.ToArray();
        }
        catch (Exception e)
        {
            DiscJockeyPlugin.LogError($"Error serializing config: {e}");
            return null;
        }
    }

    public static T DeserializeFromBytes(byte[] data)
    {
        using MemoryStream stream = new(data);

        try
        {
            return (T)serializer.ReadObject(stream);
        }
        catch (Exception e)
        {
            DiscJockeyPlugin.LogError($"Error deserializing config: {e}");
            return default;
        }
    }

    internal static void SendMessage(string label, ulong clientId, FastBufferWriter stream)
    {
        var fragment = stream.Capacity >= stream.MaxCapacity;
        var delivery = fragment ? NetworkDelivery.ReliableFragmentedSequenced : NetworkDelivery.Reliable;

        if (fragment)
            DiscJockeyPlugin.LogDebug(
                $"Size of stream ({stream.Capacity}) was past the max buffer size.\n" +
                "Config instance will be sent in fragments to avoid overflowing the buffer."
            );

        MessageManager.SendNamedMessage(label, clientId, stream, delivery);
    }
}