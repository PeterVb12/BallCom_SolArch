namespace BallCom.Warehouse.API.Models
{
    public class PickList
    {
        public Guid Id { get; set; }

        public int OrderId { get; set; }

        public string Status { get; set; } = PickListStatus.Released;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<PickListLine> Lines { get; set; } = new();
    }

    public static class PickListStatus
    {
        public const string Released = "RELEASED";
        public const string Picking = "PICKING";
        public const string Picked = "PICKED";
        public const string Packed = "PACKED";
        public const string ReadyForShipment = "READY_FOR_SHIPMENT";
        public const string Cancelled = "CANCELLED";
    }
}
