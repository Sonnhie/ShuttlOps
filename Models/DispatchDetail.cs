using System;
using System.Collections.Generic;

namespace ShuttlOps.Models;

public partial class DispatchDetail
{
    public int DispatchId { get; set; }

    public int TicketId { get; set; }

    public string VehicleStatus { get; set; } = null!;

    public string? GaRemarks { get; set; }

    public string? DriverName { get; set; }

    public string? PlateNumber { get; set; }

    public int? GaPicId { get; set; }

    public string? GaPicName { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public int? VehicleId { get; set; }

    public int? DriverId { get; set; }

    public virtual Driver? Driver { get; set; }

    public virtual TripTicket Ticket { get; set; } = null!;

    public virtual Vehicle? Vehicle { get; set; }
}
