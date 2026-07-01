namespace BallCom.Logistics.API.Models.Events
{
    public record PackageReadyEvent(Guid PickListId, int OrderId, DateTime ReadyAt);

    public record ShipmentCreatedEvent(
        Guid ShipmentId,
        int OrderId,
        string SelectedCarrier,
        decimal SelectedPrice,
        string CarrierQuotesAudit,
        string TrackingNumber,
        DateTime OccurredAt);

    public record ShipmentStatusUpdatedEvent(
        Guid ShipmentId,
        int OrderId,
        string FromStatus,
        string ToStatus,
        string Source,
        DateTime OccurredAt);
}
