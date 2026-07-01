namespace BallCom.Payment.API.Models.Events
{
    public record TransactionCreatedEvent(
        Guid TransactionId,
        int OrderId,
        decimal Amount,
        string PaymentMethod,
        DateTime OccurredAt);

    public record PaymentCompletedEvent(
        int OrderId,
        Guid TransactionId,
        decimal Amount,
        string PaymentMethod,
        DateTime CompletedAt);

    public record PaymentFailedEvent(
        int OrderId,
        Guid TransactionId,
        decimal Amount,
        string PaymentMethod,
        string Reason,
        DateTime FailedAt);
}
