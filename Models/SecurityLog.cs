using System;
using System.Collections.Generic;

namespace ShuttlOps.Models;

public partial class SecurityLog
{
    public int LogId { get; set; }

    public int TicketId { get; set; }

    public DateTime? ActualDepartureTime { get; set; }

    public DateTime? ActualArrivalTime { get; set; }

    public int? KilometerRunFrom { get; set; }

    public int? KilometerRunTo { get; set; }

    public int? GuardId { get; set; }

    public string? GuardSignatureName { get; set; }

    public DateTime? LoggedAt { get; set; }
    public string? Remarks { get; set; }

    public virtual TripTicket Ticket { get; set; } = null!;
}
