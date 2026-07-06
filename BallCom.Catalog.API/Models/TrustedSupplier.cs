namespace BallCom.Catalog.API.Models
{
    public class TrustedSupplier
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
    }
}
