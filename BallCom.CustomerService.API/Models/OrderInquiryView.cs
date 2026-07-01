namespace BallCom.CustomerService.API.Models
{
    public class OrderInquiryView
    {
        public int OrderId { get; set; }
        public string? OrderStatus { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerName { get; set; }
        public string? DeliveryStatus { get; set; }
        public string? Carrier { get; set; }
        public string? TrackingNumber { get; set; }
    }
}
