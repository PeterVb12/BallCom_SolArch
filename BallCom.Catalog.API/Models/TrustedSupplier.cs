namespace BallCom.Catalog.API.Models
{
    // DDD Aggregate binnen de bounded context 'Catalogus'.
    // Alleen geregistreerde (vertrouwde) suppliers mogen producten toevoegen.
    public class TrustedSupplier
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
    }
}
