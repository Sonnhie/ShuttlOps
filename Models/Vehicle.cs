using System;
using System.Collections.Generic;

namespace ShuttlOps.Models;

public partial class Vehicle
{
    public int VehicleId { get; set; }

    public string PlateNumber { get; set; } = null!;

    public string VehicleModel { get; set; } = null!;

    public int Capacity { get; set; }

    public string Status { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<DispatchDetail> DispatchDetails { get; set; } = [];
}
