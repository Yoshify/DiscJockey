using System;
using Unity.Netcode;
using UnityEngine;

namespace DiscJockey.Data
{
    public struct TrackMetadata : INetworkSerializable, IEquatable<TrackMetadata>
    {
        public int Index;
        public float Progress;
        public float Length;
        public string Name;

        public bool TrackSelected => Index != -1;

        public TrackMetadata(string name, int index, float length, float progress = 0)
        {
            Name = name;
            Index = index;
            Progress = progress;
            Length = length;
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Index);
                reader.ReadValueSafe(out Progress);
                reader.ReadValueSafe(out Length);
                reader.ReadValueSafe(out Name);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Index);
                writer.WriteValueSafe(Progress);
                writer.WriteValueSafe(Length);
                writer.WriteValueSafe(Name);
            }
        }

        public bool Equals(TrackMetadata other)
        {
            return Index == other.Index && Mathf.Approximately(Progress, other.Progress) && Mathf.Approximately(Length, other.Length) && Name == other.Name;
        }

        public override string ToString()
        {
            return $"TrackMetadata(Index<{Index}>, Progress<{Progress}>, Length<{Length}>, Name<{Name}>)";
        }
    }
}
