namespace BallCom.Payment.API.Models.Events
{
    public record OrderCancelledEvent(int OrderId, string Reason, DateTime CancelledAt);
}
