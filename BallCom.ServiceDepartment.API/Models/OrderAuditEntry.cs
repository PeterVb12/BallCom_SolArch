namespace BallCom.ServiceDepartment.API.Models
{
    public class OrderAuditEntry
    {
        public Guid Id { get; set; }
        public int OrderId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    public static class AuditAction
    {
        public const string Cancelled = "CANCELLED";
        public const string Modified = "MODIFIED";
    }
}
