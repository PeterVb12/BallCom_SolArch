namespace BallCom.Payment.API.Models.Commands
{
    public record StartPaymentCommand(
        int OrderId,
        string PaymentMethod,
        bool SimulateFailure = false);
}
