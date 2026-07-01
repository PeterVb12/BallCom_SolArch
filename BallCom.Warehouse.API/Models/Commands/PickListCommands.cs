namespace BallCom.Warehouse.API.Models.Commands
{
    public record StartPickingCommand(Guid PickListId);
    public record CompletePickingCommand(Guid PickListId);
    public record PackCommand(Guid PickListId);
    public record MarkReadyCommand(Guid PickListId);
}
