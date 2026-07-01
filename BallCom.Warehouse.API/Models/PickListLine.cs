namespace BallCom.Warehouse.API.Models
{
    public class PickListLine
    {
        public Guid Id { get; set; }
        public Guid PickListId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
