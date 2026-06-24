namespace BallCom.Catalog.API.Models
{
    // CQRS - COMMAND zijde: schrijf-intenties die de domeinstatus muteren.
    // Deze records komen binnen via POST endpoints.
    public record AddProductCommand(
        string Name,
        string Description,
        decimal Price,
        int Stock,
        Guid SupplierId);

    public record UpdateProductCommand(
        string Name,
        string Description,
        decimal Price,
        int Stock);

    public record RegisterSupplierCommand(
        string Name,
        string ContactEmail);
}
