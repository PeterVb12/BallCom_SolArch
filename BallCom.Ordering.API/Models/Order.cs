namespace BallCom.Ordering.API.Models
{
    // Invoer-DTO's (command-contracten) en het integratie-event. De eigenlijke
    // orderstaat leeft niet meer in een EF-entiteit maar in de event store
    // (schrijfkant) en de read models (leeskant).

    public record CustomerDetailsDto(
        string Email,
        string FullName,
        string Street,
        string City,
        string PostalCode,
        string Country);

    public record CreateOrderCommand(List<OrderItemDto> Items, CustomerDetailsDto Customer);
    public record OrderItemDto(string ProductId, int Quantity, decimal Price);

    // INTEGRATIE-event (gaat via RabbitMQ naar Payment/Warehouse). Bewust met een
    // int OrderId zodat de bestaande cross-service contracten ongewijzigd blijven.
    public record OrderPlacedEvent(int OrderId, decimal TotalPrice, DateTime CreatedAt);

    public static class OrderStatus
    {
        public const string Pending = "PENDING";
        public const string Paid = "PAID";
        public const string Processing = "PROCESSING";
        public const string Cancelled = "CANCELLED";
    }
}
