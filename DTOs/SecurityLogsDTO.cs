using System.Runtime.Versioning;

namespace ShuttlOps.DTOs
{
    public class SecurityLogsDTO
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int OdometerStart { get; set; }
        public int OdometerEnd { get; set; }
        public int GuardId { get; set; }
        public string GuardName { get; set; } = null!;
        public string Remarks { get; set; } = null!;
        public DateTime LoggedAt { get; set; }
    }
}
