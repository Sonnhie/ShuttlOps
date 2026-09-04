namespace ShuttlOps.DTOs
{
    public class NotificationMessageDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Category { get; set; } // "Ticket", "Approval", "Dispatch", "Security", "General"
        public string? Url { get; set; }
        public string? TicketNumber { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
