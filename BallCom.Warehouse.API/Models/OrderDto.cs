namespace BallCom.Warehouse.API.Models
{
    public class OrderDto
    {
        public int Id { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
