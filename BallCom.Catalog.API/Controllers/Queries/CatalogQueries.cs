namespace BallCom.Catalog.API.Queries
{
    //Geef alle producten
    public record GetAllProductsQuery();

    //Geef mij 1 specifiek product
    public record GetProductByIdQuery(Guid Id);
    
}