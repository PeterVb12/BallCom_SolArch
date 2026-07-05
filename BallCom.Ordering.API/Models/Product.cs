namespace BallCom.Ordering.API.Models
{
    public class Product
    {
        public Guid Id { get; set; } // Dit ID matcht straks met de ProductId uit de Catalogus
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public record ProductAddedIntegrationEvent
    (
        Guid ProductId,
        string Name, 
        string Description, 
        decimal Price, 
        int Stock, 
        Guid SupplierId, 
        DateTime OccurredAt
    );

    public record ProductUpdatedEvent
    (
        Guid ProductId,
        string Name,
        decimal Price,
        DateTime OccurredAt
    );

    // Integratie-event vanuit de Payment-service (RabbitMQ). Triggert in Ordering
    // het MarkPaid-command op de event-sourced Order-aggregate.
    public record PaymentCompletedIntegrationEvent
    (
        int OrderId,
        Guid TransactionId,
        decimal Amount,
        string PaymentMethod,
        DateTime CompletedAt
    );
}