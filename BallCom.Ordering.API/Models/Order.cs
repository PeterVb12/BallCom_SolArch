namespace BallCom.Ordering.API.Models
{
    public class Order
    {
        public int Id { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = OrderStatus.Pending;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

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
