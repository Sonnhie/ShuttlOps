using System;
using System.Collections.Generic;

namespace ShuttlOps.Models;

public partial class Driver
{
    public int DriverId { get; set; }

    public string DriverName { get; set; } = null!;

    public string? ContactNumber { get; set; }

    public string? LicenseNumber { get; set; }

    public string Status { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<DispatchDetail> DispatchDetails { get; set; } = new List<DispatchDetail>();
}
