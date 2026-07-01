namespace BallCom.CustomerService.API.Models
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

    public class Customer
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

    public class StoredEvent
    {
        public long Sequence { get; set; }
        public Guid AggregateId { get; set; }
        public string AggregateType { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }
}
