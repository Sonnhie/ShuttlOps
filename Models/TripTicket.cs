using System;
using System.Collections.Generic;

namespace ShuttlOps.Models;

public partial class TripTicket
{
    public int TicketId { get; set; }

    public string TicketNumber { get; set; } = null!;

    public string RequestorId { get; set; } = null!;

    public string RequestorName { get; set; } = null!;

    public string? RequestedDepartment { get; set; }

    public int? ApproverId { get; set; }

    public string? ApproverName { get; set; }

    public string ApprovalStatus { get; set; } = null!;

    public DateTime? ApprovedAt { get; set; }

    public DateTime DateRequested { get; set; }

    public DateOnly DateOfTrip { get; set; }

    public TimeOnly EstDepartureTime { get; set; }

    public TimeOnly EstArrivalTime { get; set; }

    public string PickupLocation { get; set; } = null!;

    public string DropoffLocation { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual DispatchDetail? DispatchDetail { get; set; }

    public virtual SecurityLog? SecurityLog { get; set; }

    public virtual ICollection<TripTicketPassenger> TripTicketPassengers { get; set; } = new List<TripTicketPassenger>();
}

