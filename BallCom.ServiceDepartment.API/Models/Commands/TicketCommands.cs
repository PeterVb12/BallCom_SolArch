namespace BallCom.ServiceDepartment.API.Models.Commands
{
    public record RegisterTicketCommand(
        string CustomerEmail,
        string Subject,
        string Question,
        int? OrderId = null);

    public record AnswerTicketCommand(string Answer, string AnsweredBy);

    public record ServiceCancelOrderCommand(
        string EmployeeId,
        string EmployeeName,
        string Reason);

    public record ServiceModifyOrderCommand(
        string EmployeeId,
        string EmployeeName,
        string Reason,
        List<OrderItemDto> Items);

    public record OrderItemDto(string ProductId, int Quantity, decimal Price);
}
