using System;
using System.Collections.Generic;

namespace ShuttlOps.Models;

public partial class RoleTable
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public virtual ICollection<UserTable> UserTables { get; set; } = new List<UserTable>();
}
