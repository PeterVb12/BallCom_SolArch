namespace BallCom.ServiceDepartment.API.Models
{
    public class OrderStatusView
    {
        public int OrderId { get; set; }
        public string? OrderStatus { get; set; }
        public decimal? TotalPrice { get; set; }
        public List<OrderItemView> Items { get; set; } = new();
        public string? PaymentStatus { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PickListStatus { get; set; }
        public Guid? PickListId { get; set; }
    }

    public class OrderItemView
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
