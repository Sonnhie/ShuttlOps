using System;
using System.Collections.Generic;

namespace ShuttlOps.Models;

public partial class TripTicketPassenger
{
    public int PassengerId { get; set; }

    public int TicketId { get; set; }

    public string PassengerName { get; set; } = null!;

    public virtual TripTicket Ticket { get; set; } = null!;
}
