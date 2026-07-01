namespace BallCom.ServiceDepartment.API.Models.Events
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

    public record OrderCancellationAuditedEvent(
        Guid AuditId,
        int OrderId,
        string EmployeeId,
        string EmployeeName,
        string Reason,
        DateTime OccurredAt);

    public record OrderModificationAuditedEvent(
        Guid AuditId,
        int OrderId,
        string EmployeeId,
        string EmployeeName,
        string Reason,
        string Details,
        DateTime OccurredAt);
}
