namespace BallCom.Payment.API.Models
{
    public class Transaction
    {
        public Guid Id { get; set; }

        public int OrderId { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public string Status { get; set; } = PaymentStatus.Pending;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public static class PaymentStatus
    {
        public const string Pending = "PENDING";
        public const string Paid = "PAID";
        public const string Failed = "FAILED";
    }

    public static class PaymentMethods
    {
        public const string ForwardPay = "ForwardPay";
        public const string AfterPay = "AfterPay";
    }
}
