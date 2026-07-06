namespace BallCom.Catalog.API.Models
{
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
