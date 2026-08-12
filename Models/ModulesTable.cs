using System;
using System.Collections.Generic;

namespace ShuttlOps.Models;

public partial class ModulesTable
{
    public int ModuleId { get; set; }

    public string ModuleName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<PermissionsTable> PermissionsTables { get; set; } = new List<PermissionsTable>();
}
