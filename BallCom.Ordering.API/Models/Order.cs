namespace BallCom.Ordering.API.Models
{
    public record CustomerDetailsDto(
        string Email,
        string FullName,
        string Street,
        string City,
        string PostalCode,
        string Country);

    public record CreateOrderCommand(List<OrderItemDto> Items, CustomerDetailsDto Customer);
    public record OrderItemDto(string ProductId, int Quantity, decimal Price);

    public record OrderPlacedEvent(int OrderId, decimal TotalPrice, DateTime CreatedAt);

    public static class OrderStatus
    {
        public const string Pending = "PENDING";
        public const string Paid = "PAID";
        public const string Processing = "PROCESSING";
        public const string Cancelled = "CANCELLED";
    }
}
