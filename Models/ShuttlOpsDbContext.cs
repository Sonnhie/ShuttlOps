using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ShuttlOps.Models;

public partial class ShuttlOpsDbContext : DbContext
{
    public ShuttlOpsDbContext()
    {
    }

    public ShuttlOpsDbContext(DbContextOptions<ShuttlOpsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<DispatchDetail> DispatchDetails { get; set; }

    public virtual DbSet<Driver> Drivers { get; set; }

    public virtual DbSet<ModulesTable> ModulesTables { get; set; }

    public virtual DbSet<PermissionsTable> PermissionsTables { get; set; }

    public virtual DbSet<RequestSequence> RequestSequences { get; set; }

    public virtual DbSet<RoleTable> RoleTables { get; set; }

    public virtual DbSet<SecurityLog> SecurityLogs { get; set; }

    public virtual DbSet<TripTicket> TripTickets { get; set; }

    public virtual DbSet<TripTicketPassenger> TripTicketPassengers { get; set; }

    public virtual DbSet<UserTable> UserTables { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__C2232422F49ECE26");

            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.DepartmentName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("department_name");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
        });

        modelBuilder.Entity<DispatchDetail>(entity =>
        {
            entity.HasKey(e => e.DispatchId).HasName("PK__Dispatch__434DBD55946AC33E");

            entity.HasIndex(e => e.TicketId, "UQ__Dispatch__712CC606077FA8E3").IsUnique();

            entity.Property(e => e.DriverName).HasMaxLength(100);
            entity.Property(e => e.GaPicName).HasMaxLength(100);
            entity.Property(e => e.GaRemarks).HasMaxLength(255);
            entity.Property(e => e.PlateNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.VehicleStatus)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.Driver).WithMany(p => p.DispatchDetails)
                .HasForeignKey(d => d.DriverId)
                .HasConstraintName("FK_Dispatch_Drivers");

            entity.HasOne(d => d.Ticket).WithOne(p => p.DispatchDetail)
                .HasForeignKey<DispatchDetail>(d => d.TicketId)
                .HasConstraintName("FK_Dispatch_TripTickets");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.DispatchDetails)
                .HasForeignKey(d => d.VehicleId)
                .HasConstraintName("FK_Dispatch_Vehicles");
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasKey(e => e.DriverId).HasName("PK__Drivers__F1B1CD04A3FFED4B");

            entity.Property(e => e.ContactNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DriverName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LicenseNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Available");
        });

        modelBuilder.Entity<ModulesTable>(entity =>
        {
            entity.HasKey(e => e.ModuleId);

            entity.ToTable("Modules_Table");

            entity.Property(e => e.ModuleId).HasColumnName("module_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.ModuleName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("module_name");
        });

        modelBuilder.Entity<PermissionsTable>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Permissions_Table");

            entity.Property(e => e.CanApprove).HasColumnName("can_approve");
            entity.Property(e => e.CanCreate).HasColumnName("can_create");
            entity.Property(e => e.CanDelete).HasColumnName("can_delete");
            entity.Property(e => e.CanEdit).HasColumnName("can_edit");
            entity.Property(e => e.CanView).HasColumnName("can_view");
            entity.Property(e => e.ModuleId).HasColumnName("module_id");
            entity.Property(e => e.PermissionId)
                .ValueGeneratedOnAdd()
                .HasColumnName("permission_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Module).WithMany()
                .HasForeignKey(d => d.ModuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Permissions_Module");

            entity.HasOne(d => d.Role).WithMany()
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Permissions_Role");
        });

        modelBuilder.Entity<RequestSequence>(entity =>
        {
            entity.HasKey(e => e.SeqDate).HasName("PK__request___0A2449D510831C82");

            entity.ToTable("request_sequence");

            entity.Property(e => e.SeqDate).HasColumnName("seq_date");
            entity.Property(e => e.LastNumber).HasColumnName("last_number");
        });

        modelBuilder.Entity<RoleTable>(entity =>
        {
            entity.HasKey(e => e.RoleId);

            entity.ToTable("Role_Table");

            entity.Property(e => e.RoleId)
                .ValueGeneratedNever()
                .HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<SecurityLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Security__5E548648C52A4ACF");

            entity.HasIndex(e => e.TicketId, "UQ__Security__712CC6061AB87C72").IsUnique();

            entity.Property(e => e.GuardSignatureName).HasMaxLength(100);

            entity.HasOne(d => d.Ticket).WithOne(p => p.SecurityLog)
                .HasForeignKey<SecurityLog>(d => d.TicketId)
                .HasConstraintName("FK_SecurityLogs_TripTickets");
        });

        modelBuilder.Entity<TripTicket>(entity =>
        {
            entity.HasKey(e => e.TicketId).HasName("PK__TripTick__712CC607A471500A");

            entity.HasIndex(e => e.TicketNumber, "UQ__TripTick__CBED06DAD8D42A22").IsUnique();

            entity.Property(e => e.ApprovalStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.ApproverName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DateRequested).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DropoffLocation).HasMaxLength(150);
            entity.Property(e => e.PickupLocation).HasMaxLength(150);
            entity.Property(e => e.RequestedDepartment).HasMaxLength(50);
            entity.Property(e => e.RequestorId).HasMaxLength(50);
            entity.Property(e => e.RequestorName).HasMaxLength(100);
            entity.Property(e => e.TicketNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TripTicketPassenger>(entity =>
        {
            entity.HasKey(e => e.PassengerId).HasName("PK__TripTick__88915FB03302FCBC");

            entity.Property(e => e.PassengerName).HasMaxLength(100);

            entity.HasOne(d => d.Ticket).WithMany(p => p.TripTicketPassengers)
                .HasForeignKey(d => d.TicketId)
                .HasConstraintName("FK_Passengers_TripTickets");
        });

        modelBuilder.Entity<UserTable>(entity =>
        {
            entity.ToTable("User_Table");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.EmailAdd)
                .HasMaxLength(50)
                .HasColumnName("email_add");
            entity.Property(e => e.EmployeeName).HasColumnName("employee_name");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .HasColumnName("user_name");

            entity.HasOne(d => d.Department).WithMany(p => p.UserTables)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Department");

            entity.HasOne(d => d.Role).WithMany(p => p.UserTables)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Role");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId).HasName("PK__Vehicles__476B54925BAD2E56");

            entity.HasIndex(e => e.PlateNumber, "UQ__Vehicles__03692624912FC991").IsUnique();

            entity.Property(e => e.Capacity).HasDefaultValue(4);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PlateNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Available");
            entity.Property(e => e.VehicleModel).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
