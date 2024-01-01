using System;
using Unity.Netcode;
using UnityEngine;

namespace DiscJockey.Data
{
    public class NetworkedConfig : INetworkSerializable, IEquatable<NetworkedConfig>
    {
        public bool ClientsCanDownloadSongs;
        public float MaxSongDuration;

        public NetworkedConfig(bool clientsCanDownloadSongs, float maxSongDuration)
        {
            ClientsCanDownloadSongs = clientsCanDownloadSongs;
            MaxSongDuration = maxSongDuration;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out ClientsCanDownloadSongs);
                reader.ReadValueSafe(out MaxSongDuration);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(ClientsCanDownloadSongs);
                writer.WriteValueSafe(MaxSongDuration);
            }
        }

        public bool Equals(NetworkedConfig other) => 
            other != null && ClientsCanDownloadSongs == other.ClientsCanDownloadSongs && Mathf.Approximately(MaxSongDuration, other.MaxSongDuration);
        
        public override string ToString() => $"NetworkedConfig(ClientsCanDownloadSongs<{ClientsCanDownloadSongs}>, MaxSongDuration<{MaxSongDuration}>)";
    }
}