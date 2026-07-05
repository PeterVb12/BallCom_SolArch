namespace BallCom.Ordering.API.Models
{
    // Eén rij in de append-only event store (de SCHRIJFKANT / C van CQRS).
    // Dit is de enige bron van waarheid van de Ordering-service; er bestaat
    // geen aparte "Orders"-statustabel meer aan de schrijfkant.
    public class StoredEvent
    {
        // Globale, oplopende volgorde over alle streams heen (voor replay/projectie).
        public long Sequence { get; set; }

        // Identificeert de aggregate-instantie (hier: het order-id als tekst).
        public string StreamId { get; set; } = string.Empty;

        public string AggregateType { get; set; } = string.Empty;

        // Positie binnen de stream van deze aggregate (1,2,3,...) voor optimistic concurrency.
        public int Version { get; set; }

        public string EventType { get; set; } = string.Empty;

        // Het geserialiseerde domein-event (onveranderlijk feit).
        public string Payload { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; }
    }
}
