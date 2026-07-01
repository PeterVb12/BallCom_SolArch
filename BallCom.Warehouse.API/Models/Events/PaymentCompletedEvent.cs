namespace BallCom.Warehouse.API.Models.Events
{
    public record PaymentCompletedEvent(
        int OrderId,
        Guid TransactionId,
        decimal Amount,
        string PaymentMethod,
        DateTime CompletedAt);
}
