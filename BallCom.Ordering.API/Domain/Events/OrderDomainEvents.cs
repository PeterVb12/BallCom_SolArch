namespace BallCom.Ordering.API.Domain.Events
{
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
