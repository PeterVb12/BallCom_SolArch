namespace BallCom.ServiceDepartment.API.Models
{
    public class Ticket
    {
        public Guid Id { get; set; }
        public int? OrderId { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string? Answer { get; set; }
        public string Status { get; set; } = TicketStatus.Open;
        public string? AnsweredBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AnsweredAt { get; set; }
    }

    public static class TicketStatus
    {
        public const string Open = "OPEN";
        public const string Answered = "ANSWERED";
        public const string Closed = "CLOSED";
    }
}
