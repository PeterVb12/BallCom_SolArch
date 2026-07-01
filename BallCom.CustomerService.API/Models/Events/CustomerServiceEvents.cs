namespace BallCom.CustomerService.API.Models.Events
{
    public record TicketRegisteredEvent(
        Guid TicketId,
        int? OrderId,
        string CustomerEmail,
        string Subject,
        string Question,
        DateTime OccurredAt);

    public record TicketAnsweredEvent(
        Guid TicketId,
        string Answer,
        string AnsweredBy,
        DateTime OccurredAt);
}
