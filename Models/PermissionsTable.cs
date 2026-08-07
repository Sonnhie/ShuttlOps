using System;
using System.Collections.Generic;

namespace ShuttlOps.Models;

public partial class PermissionsTable
{
    public int PermissionId { get; set; }

    public int RoleId { get; set; }

    public int ModuleId { get; set; }

    public bool CanView { get; set; }

    public bool CanCreate { get; set; }

    public bool CanEdit { get; set; }

    public bool CanDelete { get; set; }

    public bool CanApprove { get; set; }

    public virtual ModulesTable Module { get; set; } = null!;

    public virtual RoleTable Role { get; set; } = null!;
}
