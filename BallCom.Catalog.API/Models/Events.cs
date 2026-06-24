namespace BallCom.Catalog.API.Models
{
    // Domein-events die zowel (1) worden opgeslagen in de Event Store als
    // (2) worden gepubliceerd op RabbitMQ (Event Driven Architecture).
    // Downstream services (Ordering) consumeren deze om eventueel consistent
    // hun eigen productreferenties bij te houden.
    public record ProductAddedEvent(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int Stock,
        Guid SupplierId,
        DateTime OccurredAt);

    public record ProductUpdatedEvent(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int Stock,
        DateTime OccurredAt);

    public record SupplierRegisteredEvent(
        Guid SupplierId,
        string Name,
        string ContactEmail,
        DateTime OccurredAt);
}
