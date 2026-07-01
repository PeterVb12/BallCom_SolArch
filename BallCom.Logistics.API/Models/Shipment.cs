namespace BallCom.Logistics.API.Models
{
    public class Shipment
    {
        public Guid Id { get; set; }
        public int OrderId { get; set; }
        public string Status { get; set; } = ShipmentStatus.Created;
        public string SelectedCarrier { get; set; } = string.Empty;
        public decimal SelectedPrice { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string CarrierQuotesAudit { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public static class ShipmentStatus
    {
        public const string Created = "CREATED";
        public const string InTransit = "IN_TRANSIT";
        public const string Delivered = "DELIVERED";
    }
}
