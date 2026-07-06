namespace BallCom.Ordering.API.ReadModels
{
    public class OrderSummary
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public DateTime PlacedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    public class OrderLineView
    {
        public long Id { get; set; }
        public int OrderId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class CustomerOrderStat
    {
        public string CustomerEmail { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime LastOrderAt { get; set; }
    }
}
