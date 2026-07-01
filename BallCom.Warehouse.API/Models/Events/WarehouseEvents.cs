namespace BallCom.Warehouse.API.Models.Events
{
    public record PickListCreatedEvent(
        Guid PickListId,
        int OrderId,
        DateTime OccurredAt);

    public record PickListStatusChangedEvent(
        Guid PickListId,
        int OrderId,
        string FromStatus,
        string ToStatus,
        DateTime OccurredAt);

    public record PackageReadyEvent(
        Guid PickListId,
        int OrderId,
        DateTime ReadyAt);
}
