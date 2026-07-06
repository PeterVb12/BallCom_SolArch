namespace BallCom.Catalog.API.Models
{
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
