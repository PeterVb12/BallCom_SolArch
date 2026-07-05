namespace BallCom.Ordering.API.Domain.Events
{
    // Domein-events vormen de SCHRIJFKANT (C) van de Ordering-service.
    // Ze zijn de enige bron van waarheid: de staat van een order bestaat NIET
    // als losse tabelrij, maar wordt in code opgebouwd door deze events
    // achter elkaar af te spelen (rehydratie). Zie OrderAggregate.Apply(...).
    //
    // LET OP: dit zijn INTERNE domein-events (append-only event store).
    // Ze staan los van de INTEGRATIE-events die via RabbitMQ naar andere
    // microservices gaan (zie Models/Order.cs -> OrderPlacedEvent).
    public interface IOrderEvent
    {
        int OrderId { get; }
        DateTime OccurredAt { get; }
    }

    public record OrderLineData(string ProductId, int Quantity, decimal Price);

    public record OrderPlacedDomainEvent(
        int OrderId,
        string CustomerEmail,
        string CustomerName,
        string Street,
        string City,
        string PostalCode,
        string Country,
        IReadOnlyList<OrderLineData> Items,
        decimal TotalPrice,
        DateTime OccurredAt) : IOrderEvent;

    public record OrderPaidDomainEvent(
        int OrderId,
        decimal Amount,
        DateTime OccurredAt) : IOrderEvent;

    public record OrderProcessingStartedDomainEvent(
        int OrderId,
        DateTime OccurredAt) : IOrderEvent;

    public record OrderCancelledDomainEvent(
        int OrderId,
        string Reason,
        DateTime OccurredAt) : IOrderEvent;
}
