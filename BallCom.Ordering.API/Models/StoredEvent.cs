namespace BallCom.Ordering.API.Models
{
    public class StoredEvent
    {
        public long Sequence { get; set; }

        public string StreamId { get; set; } = string.Empty;

        public string AggregateType { get; set; } = string.Empty;

        public int Version { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string Payload { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; }
    }
}
