using System;
using System.Collections.Generic;

namespace ShuttlOps.Models;

public partial class Department
{
    public int DepartmentId { get; set; }

    public string? DepartmentName { get; set; }

    public int? ManagerId { get; set; }

    public virtual ICollection<UserTable> UserTables { get; set; } = new List<UserTable>();
}
