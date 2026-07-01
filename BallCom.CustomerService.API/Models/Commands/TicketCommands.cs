namespace BallCom.CustomerService.API.Models.Commands
{
    public record RegisterTicketCommand(
        string CustomerEmail,
        string Subject,
        string Question,
        int? OrderId = null);

    public record AnswerTicketCommand(string Answer, string AnsweredBy);
}
