using System;
using Unity.Netcode;

namespace DiscJockey.Data
{
    public struct BoomboxMetadata : INetworkSerializable, IEquatable<BoomboxMetadata>
    {
        public TrackMetadata CurrentTrackMetadata;
        public TrackMode TrackMode;

        public BoomboxMetadata(TrackMetadata currentTrackMetadata, TrackMode trackMode = TrackMode.Sequential)
        {
            CurrentTrackMetadata = currentTrackMetadata;
            TrackMode = trackMode;
        }

        public void UpdateTrackMetadata(TrackMetadata newTrackMetadata)
        {
            CurrentTrackMetadata = newTrackMetadata;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out CurrentTrackMetadata);
                reader.ReadValueSafe(out TrackMode);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(CurrentTrackMetadata);
                writer.WriteValueSafe(TrackMode);
            }
        }

        public bool Equals(BoomboxMetadata other)
        {
            return CurrentTrackMetadata.Equals(other.CurrentTrackMetadata) && TrackMode == other.TrackMode;
        }

        public static BoomboxMetadata Empty()
        {
            return new BoomboxMetadata
            {
                CurrentTrackMetadata = new TrackMetadata(string.Empty, -1, 0)
            };
        }

        public override string ToString()
        {
            return $"BoomboxMetadata(TrackMode<{TrackMode}>, CurrentTrack<{CurrentTrackMetadata}>";
        }
    }
}
