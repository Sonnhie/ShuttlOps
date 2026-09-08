using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShuttlOps.DTOs;
using ShuttlOps.Models;
using ShuttlOps.Services.Interfaces;
using System.Security.Claims;

namespace ShuttlOps.Services.MainServices
{
    [Authorize]
    public class GAServices(
        ShuttlOpsDbContext dbContext,
        ILogger<GAServices> logger,
        IHttpContextAccessor httpContextAccessor,
        INotificationService? notificationService = null) : IGAService
    {

        public async Task<List<VehicleDTO>> GetAllVehicle()
        {
            try
            {
                var vehicle = await dbContext
                              .Vehicles
                              .Select(v => new VehicleDTO
                              {
                                  Id = v.VehicleId,
                                  VehicleModel = v.VehicleModel,
                                  PlateNumber = v.PlateNumber,
                                  Capacity = v.Capacity,
                                  Status = v.Status
                              })
                              .ToListAsync();
                return vehicle;
            }
            catch
            {
                return [];
            }
        }

        public async Task<List<DriverDTO>> GetAllDriver()
        {
            try
            {
                var driver = await dbContext
                              .Drivers
                              .Select(d => new DriverDTO
                              {
                                  Id = d.DriverId,
                                  DriverName = d.DriverName,
                                  ContactNumber = d.ContactNumber ?? "Unknown",
                                  LicenseNumber = d.LicenseNumber ?? "Unknown",
                                  Status = d.Status
                              })
                              .ToListAsync();
                return driver;
            }
            catch
            {
                return [];
            }
        }

        public async Task<List<TripTicketResponseDTO>> GetApproveRequest()
        {
            try
            {
                var ApproveTicketList = await dbContext
                                        .TripTickets
                                        .Where(t => t.ApprovalStatus == "Ready for Dispatch")
                                        .Select(t => new TripTicketResponseDTO
                                        {
                                            TicketNumber = t.TicketNumber,
                                            TripDate = t.DateOfTrip,
                                            DropLocation = t.DropoffLocation
                                        })
                                        .ToListAsync();

                

                return ApproveTicketList;
            }
            catch
            {
                return [];
            }
        }

        public async Task<(bool isSuccesss, string message)> UpdateDriverDetails(DriverDTO Driver)
        {
            try
            {
                var isExist = await dbContext.Drivers.FindAsync(Driver.Id);
                if (isExist == null)
                {
                    return (false, "Driver not registered on the database.");
                }

                isExist.DriverName = Driver.DriverName;
                isExist.LicenseNumber = Driver.LicenseNumber;
                isExist.ContactNumber = Driver.ContactNumber;
                isExist.Status = Driver.Status;

                await dbContext.SaveChangesAsync();
                return (true, "Driver details successfully updated.");
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error creating request");
                return (false, $"Error creating request: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> DeleteDriverDetails(int id)
        {
            try
            {
                var isExist = await dbContext.Drivers.FindAsync(id);
                if (isExist == null) return (false, "Driver not exist on the database.");
                dbContext.Drivers.Remove(isExist);
                await dbContext.SaveChangesAsync();
                return (true, "Driver successfully deleted on the database.");
            }catch(Exception ex)
            {
                logger.LogError(ex, "Error creating request");
                return (false, $"Error creating request: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> CreateDriverDetails(DriverDTO Driver)
        {
            try
            {
                if (Driver == null) return (false, "Driver details is empty.");

                var newDriverRecord = new Driver
                {
                    DriverName = Driver.DriverName,
                    LicenseNumber = Driver.LicenseNumber,
                    ContactNumber = Driver.ContactNumber,
                    Status = Driver.Status,
                    IsActive = true
                };

                dbContext.Drivers.Add(newDriverRecord);
                await dbContext.SaveChangesAsync();
                return (true, "New Driver successfully registered.");
            }catch(Exception ex)
            {
                logger.LogError(ex, "Error creating request");
                return (false, $"Error creating request: {ex.Message}");
            }
        }

        public async Task<(bool isSuccesss, string message)> CreateVehicleDetails(VehicleDTO Vehicle)
        {
            try
            {
                if (Vehicle == null) return (false, "Vehicle details is empty.");
                var newVehicle = new Vehicle
                {
                    VehicleModel = Vehicle.VehicleModel,
                    PlateNumber = Vehicle.PlateNumber,
                    Capacity = Vehicle.Capacity,
                    Status = Vehicle.Status,
                    IsActive= true
                };

                dbContext.Vehicles.Add(newVehicle);
                await dbContext.SaveChangesAsync();
                return (true, "Vehicle details successfully updated.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating request");
                return (false, $"Error creating request: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> UpdateVehicleDetails(VehicleDTO Vehicle)
        {
            try
            {
                var isExist = await dbContext.Vehicles.FindAsync(Vehicle.Id);
                if (isExist == null) return (false, "Vehicle does not exist on the database.");

                isExist.VehicleModel = Vehicle.VehicleModel;
                isExist.PlateNumber = Vehicle.PlateNumber;
                isExist.Capacity = Vehicle.Capacity;
                isExist.Status = Vehicle.Status;
                
                await dbContext.SaveChangesAsync();
                return (true, "Vehicle successfully registered.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating request");
                return (false, $"Error creating request: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> DeleteVehicleDetails(int id)
        {
            try
            {
                var isExist = await dbContext.Vehicles.FindAsync(id);
                if (isExist == null) return (false, "Vehicle does not exist on the database.");

                dbContext.Vehicles.Remove(isExist);
                await dbContext.SaveChangesAsync();

                return (true, "Vehicle successfully deleted on the database.");
            }catch (Exception ex)
            {
                logger.LogError(ex, "Error creating request");
                return (false, $"Error creating request: {ex.Message}");
            }
        }

        public async Task<UserDto> GetUserInfo()
        {
            var httpContext = httpContextAccessor.HttpContext;
            try
            {
                var userid = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userinfo = await dbContext.UserTables
                                .Where(u => u.Id == Convert.ToInt32(userid))
                                .Select(u => new UserDto
                                {
                                    Id = u.Id,
                                    EmployeeName = u.EmployeeName,
                                    Role = u.Role.RoleName,
                                    Department = u.Department.DepartmentName ?? ""
                                })
                                .FirstOrDefaultAsync();
                return userinfo ?? new();
            }
            catch
            {
                return new();
            }

        }

       
        public async Task<(bool isSuccess, string message)> AssignDriver(DispatchDto Dispatch)
        {
            try
            {
                var user = await GetUserInfo();
                var driver = await dbContext.Drivers.FindAsync(Dispatch.DriverId);
                var vehicle = await dbContext.Vehicles.FindAsync(Dispatch.VehicleId);

                if (user == null)
                    return (false, "User not found or unauthenticated.");

                if (Dispatch.TicketId <= 0)
                    return (false, $"Invalid Ticket ID: {Dispatch.TicketId}");

                var isTicketExist = await dbContext.TripTickets
                                .FindAsync(Dispatch.TicketId);

                if (isTicketExist == null) return (false, "Ticket not exist.");
                if (driver == null) return (false, "Driver does not exist.");
                if (vehicle == null) return (false, "Vehicle does not exist.");

                // Block if driver is In Transit
                if (driver.Status == "In Transit")
                    return (false, $"Driver {driver.DriverName} is currently In Transit and cannot be assigned.");

                // Block if vehicle is In Transit
                if (vehicle.Status == "In Transit")
                    return (false, $"Vehicle {vehicle.PlateNumber} is currently In Transit and cannot be assigned.");

                // Prevent duplicate assignment on the same ticket
                var existingDispatch = await dbContext.DispatchDetails
                    .FirstOrDefaultAsync(d => d.TicketId == Dispatch.TicketId);

                if (existingDispatch != null)
                    return (false, "This ticket already has an assigned driver and vehicle.");

                // Time-conflict: same driver on same date with overlapping time window
                var driverConflict = await dbContext.DispatchDetails
                    .Include(d => d.Ticket)
                    .Where(d => d.DriverId == Dispatch.DriverId
                             && d.TicketId != Dispatch.TicketId
                             && d.Ticket.DateOfTrip == isTicketExist.DateOfTrip
                             && d.Ticket.ApprovalStatus != "Completed"
                             && d.Ticket.ApprovalStatus != "Cancelled"
                             && d.Ticket.ApprovalStatus != "Denied"
                             && d.Ticket.EstDepartureTime < isTicketExist.EstArrivalTime
                             && d.Ticket.EstArrivalTime > isTicketExist.EstDepartureTime)
                    .FirstOrDefaultAsync();

                if (driverConflict != null)
                    return (false, $"Driver {driver.DriverName} is already assigned to ticket {driverConflict.Ticket.TicketNumber} on {isTicketExist.DateOfTrip} (Time: {driverConflict.Ticket.EstDepartureTime:hh:mm} - {driverConflict.Ticket.EstArrivalTime:hh:mm}).");

                // Time-conflict: same vehicle on same date with overlapping time window
                var vehicleConflict = await dbContext.DispatchDetails
                    .Include(d => d.Ticket)
                    .Where(d => d.VehicleId == Dispatch.VehicleId
                             && d.TicketId != Dispatch.TicketId
                             && d.Ticket.DateOfTrip == isTicketExist.DateOfTrip
                             && d.Ticket.ApprovalStatus != "Completed"
                             && d.Ticket.ApprovalStatus != "Cancelled"
                             && d.Ticket.ApprovalStatus != "Denied"
                             && d.Ticket.EstDepartureTime < isTicketExist.EstArrivalTime
                             && d.Ticket.EstArrivalTime > isTicketExist.EstDepartureTime)
                    .FirstOrDefaultAsync();

                if (vehicleConflict != null)
                    return (false, $"Vehicle {vehicle.PlateNumber} is already assigned to ticket {vehicleConflict.Ticket.TicketNumber} on {isTicketExist.DateOfTrip} (Time: {vehicleConflict.Ticket.EstDepartureTime:hh:mm} - {vehicleConflict.Ticket.EstArrivalTime:hh:mm}).");

                // All checks passed
                driver.Status = "Assigned";
                vehicle.Status = "Assigned";

                var newDispatchRecord = new DispatchDetail
                {
                    TicketId = Dispatch.TicketId,
                    VehicleStatus = "Assigned",
                    GaRemarks = Dispatch.Remarks,
                    DriverName = Dispatch.DriverName,
                    PlateNumber = Dispatch.PlateNumber,
                    GaPicId = user.Id,
                    GaPicName = user.EmployeeName,
                    ConfirmedAt = DateTime.UtcNow,
                    VehicleId = Dispatch.VehicleId,
                    DriverId = Dispatch.DriverId
                };  

                dbContext.DispatchDetails.Add(newDispatchRecord);

                isTicketExist.UpdatedAt = DateTime.UtcNow;
                isTicketExist.ApprovalStatus = string.IsNullOrWhiteSpace(Dispatch.Status)
                    ? "Ready for Dispatch"
                    : Dispatch.Status;

                await dbContext.SaveChangesAsync();

                if (notificationService != null)
                {
                    if (!string.IsNullOrEmpty(isTicketExist.RequestorId))
                    {
                        await notificationService.SendToUserAsync(isTicketExist.RequestorId, new NotificationMessageDTO
                        {
                            Title = "Driver & Vehicle Assigned",
                            Message = $"Driver {Dispatch.DriverName} and vehicle {Dispatch.PlateNumber} assigned to ticket {isTicketExist.TicketNumber}.",
                            Category = "Dispatch",
                            TicketNumber = isTicketExist.TicketNumber,
                            Url = "/User/TripSchedule"
                        });
                    }

                    await notificationService.SendToRoleAsync("Security", new NotificationMessageDTO
                    {
                        Title = "New Dispatch Clearance Ready",
                        Message = $"Ticket {isTicketExist.TicketNumber} is ready for dispatch clearance.",
                        Category = "Dispatch",
                        TicketNumber = isTicketExist.TicketNumber,
                        Url = "/Security/SecurityLogs"
                    });

                    await notificationService.SendToSectionHeadOfDepartmentAsync(isTicketExist.RequestedDepartment ?? "", new NotificationMessageDTO
                    {
                        Title = "Driver & Vehicle Assigned",
                        Message = $"Driver {Dispatch.DriverName} and vehicle {Dispatch.PlateNumber} assigned to ticket {isTicketExist.TicketNumber}.",
                        Category = "Dispatch",
                        TicketNumber = isTicketExist.TicketNumber,
                        Url = "/User/TripSchedule"
                    });
                }

                return (true, "Dispatch Details successfully created.");

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating request");
                return (false, $"Error creating request: {ex.Message}");
            }
        }


    }
}


