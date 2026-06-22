namespace BallCom.Ordering.API.Models
{
    public class Order
    {
        public int Id { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "PENDING";
        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public record CreateOrderCommand(List<OrderItemDto> Items);
    public record OrderItemDto(string ProductId, int Quantity, decimal Price);
}
