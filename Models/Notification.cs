using System;
using System.Collections.Generic;

namespace ShuttlOps.Models.Temp;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string? TicketNumber { get; set; }

    public string Url { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}
