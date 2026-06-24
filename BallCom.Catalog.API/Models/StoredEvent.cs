namespace BallCom.Catalog.API.Models
{
    // Event Sourcing - de append-only EventStore tabel.
    // Elke product-mutatie wordt hier als onveranderlijk feit opgeslagen.
    // Het read model (Products tabel) wordt vanuit deze events geprojecteerd.
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
