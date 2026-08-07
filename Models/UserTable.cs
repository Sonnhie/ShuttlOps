using System;
using System.Collections.Generic;

namespace ShuttlOps.Models;

public partial class UserTable
{
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string EmployeeName { get; set; } = null!;

    public string EmailAdd { get; set; } = null!;

    public int RoleId { get; set; }

    public int DepartmentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual RoleTable Role { get; set; } = null!;
}
