namespace BallCom.Payment.API.Models.Events
{
    public record OrderPlacedEvent(
        int OrderId,
        decimal TotalPrice,
        DateTime CreatedAt);
}
