namespace BallCom.Payment.API.Models
{
    public class StoredEvent
    {
        public long Sequence { get; set; }
        public Guid AggregateId { get; set; }
        public string AggregateType { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }
}
