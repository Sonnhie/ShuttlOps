using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShuttlOps.DTOs;
using ShuttlOps.Models;
using ShuttlOps.Services.Interfaces;
using System.Security.Claims;

namespace ShuttlOps.Services.MainServices
{
    [Authorize]
    public class RequestService(
        ShuttlOpsDbContext dbContext,
        ILogger<RequestService> logger,
        IHttpContextAccessor httpContextAccessor,
        INotificationService? notificationService = null
    ) : IRequestService
    {
        public async Task<(bool IsSuccess, string Message)> CreateRequest([FromBody] CreateTripTicketDTO ticketDTO)
        {
            //await ResolveTransitStatuses();
            try
            {
                if (ticketDTO == null)
                {
                    return (false, "Invalid request payload.");
                }

                if (ticketDTO.TripDate < DateOnly.FromDateTime(DateTime.Today))
                {
                    return (false, "Trip date cannot be in the past.");
                }

                if (ticketDTO.TripDate == DateOnly.FromDateTime(DateTime.Today))
                {
                    if (ticketDTO.DepartureTime <= TimeOnly.FromDateTime(DateTime.Now))
                    {
                        return (false, "Departure time cannot be in the past.");
                    }
                }


                if (string.IsNullOrWhiteSpace(ticketDTO.PickupLocation))
                {
                    return (false, "Pickup location is required.");
                }

                if (string.IsNullOrWhiteSpace(ticketDTO.DropLocation))
                {
                    return (false, "Drop-off location is required.");
                }

                if (string.IsNullOrWhiteSpace(ticketDTO.Purpose))
                {
                    return (false, "Purpose is required.");
                }

                if (ticketDTO.DepartureTime >= ticketDTO.ArrivalTime)
                {
                    return (false, "Estimated arrival time must be later than estimated departure time.");
                }

                var httpContext = httpContextAccessor.HttpContext;
                var userIdClaim = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var parsedUserId))
                {
                    return (false, "User session is invalid or unauthenticated.");
                }

                var userinfo = await dbContext.UserTables
                    .Include(u => u.Department)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == parsedUserId);

                if (userinfo == null)
                {
                    return (false, $"Requestor information for user ID {parsedUserId} was not found.");
                }

                if (userinfo.Role.RoleName == "Security")
                {
                    return (false, "Requestor lacks access to trip management.");
                }

                var activeDriversCount = await dbContext.Drivers
                    .Where(d => d.Status != "Maintenance" && d.Status != "On Leave" && d.Status != "Suspended" && d.Status != "Assigned" && d.Status != "In Transit")
                    .CountAsync();

                var activeVehiclesCount = await dbContext.Vehicles
                    .Where(v => v.Status != "In Transit" && v.Status != "Under Maintenance" && v.Status != "Decommissioned" && v.Status != "Assigned")
                    .CountAsync();

                var overlappingTrips = await dbContext.TripTickets
                    .Include(t => t.DispatchDetail)
                    .Where(t => t.DateOfTrip == ticketDTO.TripDate && t.EstDepartureTime < ticketDTO.ArrivalTime && t.EstArrivalTime > ticketDTO.DepartureTime && t.ApprovalStatus != "Denied" && t.ApprovalStatus != "Cancelled" && t.ApprovalStatus != "Rejected")
                    .ToListAsync();

                var busyDriversCount = overlappingTrips.Where(t => t.DispatchDetail != null && t.DispatchDetail.DriverId.HasValue).Select(t => t.DispatchDetail.DriverId).Distinct().Count();
                var busyVehiclesCount = overlappingTrips.Where(t => t.DispatchDetail != null && t.DispatchDetail.VehicleId.HasValue).Select(t => t.DispatchDetail.VehicleId).Distinct().Count();

                if (activeVehiclesCount - busyVehiclesCount <= 0) return (false, "No available shuttles for the requested time and route.");
                if (activeDriversCount - busyDriversCount <= 0) return (false, "No available drivers for the requested time and route.");

                int seq = await GenerateTicketId();
                if (seq <= 0)
                {
                    // Fallback to request_sequence table count
                    var todayOnly = DateOnly.FromDateTime(DateTime.Today);
                    var todaySeq = await dbContext.RequestSequences.FirstOrDefaultAsync(s => s.SeqDate == todayOnly);
                    if (todaySeq != null)
                    {
                        todaySeq.LastNumber += 1;
                        seq = todaySeq.LastNumber;
                    }
                    else
                    {
                        seq = 1;
                        dbContext.RequestSequences.Add(new RequestSequence { SeqDate = todayOnly, LastNumber = 1 });
                    }
                    await dbContext.SaveChangesAsync();
                }

                string ticketNumber = $"REQ-{DateTime.Now:yyyyMMdd}-{seq:D4}";

                var passengers = (ticketDTO.Passengers ?? new List<PassengerDTO>())
                    .Where(p => !string.IsNullOrWhiteSpace(p.PassengerName))
                    .Select(p => new TripTicketPassenger
                    {
                        PassengerName = p.PassengerName.Trim()
                    })
                    .ToList();

                var ticket = new TripTicket
                {
                    TicketNumber = ticketNumber,
                    RequestorId = userinfo.UserName,
                    RequestorName = userinfo.EmployeeName ?? userinfo.UserName,
                    ApprovalStatus = "Pending Section Head",
                    DateRequested = DateTime.Now,
                    DateOfTrip = ticketDTO.TripDate,
                    RequestedDepartment = userinfo.Department?.DepartmentName ?? "General",
                    EstDepartureTime = ticketDTO.DepartureTime,
                    EstArrivalTime = ticketDTO.ArrivalTime,
                    PickupLocation = ticketDTO.PickupLocation.Trim(),
                    DropoffLocation = ticketDTO.DropLocation.Trim(),
                    Purpose = ticketDTO.Purpose.Trim(),
                    CreatedAt = DateTime.Now,
                    TripTicketPassengers = passengers
                };

                await dbContext.TripTickets.AddAsync(ticket);
                await dbContext.SaveChangesAsync();

                if (notificationService != null)
                {
                    await notificationService.SendToRoleAsync("GA", new NotificationMessageDTO
                    {
                        Title = "New Trip Request",
                        Message = $"Ticket {ticketNumber} requested by {userinfo.EmployeeName ?? userinfo.UserName} ({userinfo.Department?.DepartmentName ?? "General"}) for {ticketDTO.TripDate:yyyy-MM-dd}.",
                        Category = "Ticket",
                        TicketNumber = ticketNumber,
                        Url = "/User/TripSchedule"
                    });
                    await notificationService.SendToRoleInDepartmentAsync("Section Approver", userinfo.Department?.DepartmentName ?? "General", new NotificationMessageDTO
                    {
                        Title = "Section Approval Required",
                        Message = $"New trip ticket {ticketNumber} submitted by {userinfo.EmployeeName ?? userinfo.UserName} for {ticketDTO.TripDate:yyyy-MM-dd}.",
                        Category = "Approval",
                        TicketNumber = ticketNumber,
                        Url = "/User/TripSchedule"
                    });
                }
                return (true, $"Trip ticket {ticketNumber} created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating trip ticket request.");
                return (false, $"Error creating request: {ex.Message}");
            }
        }

        public async Task<int> GenerateTicketId()
        {
            try
            {
                var result = await dbContext.Database
                    .SqlQueryRaw<int>("EXEC [dbo].[GetNextRequestId]")
                    .ToListAsync();
                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "GetNextRequestId stored procedure execution failed. Falling back to sequence table.");
                return 0;
            }
        }

        public async Task<UserDto> GetUserInfo()
        {
            var httpContext = httpContextAccessor.HttpContext;
            try
            {
                var userid = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userid) || !int.TryParse(userid, out var parsedId))
                {
                    return new();
                }

                var userinfo = await dbContext.UserTables
                    .Where(u => u.Id == parsedId)
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

        public async Task<List<TripTicketResponseDTO>> GetAllRequests()
        {
            var query = dbContext.TripTickets.AsQueryable();
            //  await ResolveTransitStatuses();
            var userinfo = await GetUserInfo();

            if (userinfo.Role == "Requestor" || userinfo.Role == "Section Approver")
            {
                query = query.Where(t => t.RequestedDepartment == userinfo.Department);
            }

            var requests = await query
                .Include(t => t.TripTicketPassengers)
                .Include(t => t.DispatchDetail)
                .Select(t => new TripTicketResponseDTO
                {
                    TicketId = t.TicketId,
                    TicketNumber = t.TicketNumber,
                    RequestDate = DateOnly.FromDateTime(t.DateRequested),
                    RequestDepartment = t.RequestedDepartment ?? "",
                    TripDate = t.DateOfTrip,
                    DepartureTime = t.EstDepartureTime,
                    ArrivalTime = t.EstArrivalTime,
                    PickupLocation = t.PickupLocation,
                    DropLocation = t.DropoffLocation,
                    Purpose = t.Purpose,
                    Remarks = null,
                    DriverName = t.DispatchDetail.DriverName ?? "",
                    ApprovalStatus = t.ApprovalStatus,
                    Requestor = t.RequestorName,
                    Passengers = t.TripTicketPassengers.Select(p => new PassengerDTO
                    {
                        PassengerName = p.PassengerName
                    }).ToList(),
                })
                .ToListAsync();

            return requests;
        }


        public async Task<List<TripTicketResponseDTO>> GetScheduledTrip()
        {
            var query = dbContext.TripTickets.AsQueryable();
            //  await ResolveTransitStatuses();
            var userinfo = await GetUserInfo();

            var requests = await query
                .Include(t => t.TripTicketPassengers)
                .Include(t => t.DispatchDetail)
                .Where(t => t.ApprovalStatus != "Cancelled" && t.ApprovalStatus != "Pending Section Head")
                .Select(t => new TripTicketResponseDTO
                {
                    TicketId = t.TicketId,
                    TicketNumber = t.TicketNumber,
                    RequestDate = DateOnly.FromDateTime(t.DateRequested),
                    RequestDepartment = t.RequestedDepartment ?? "",
                    TripDate = t.DateOfTrip,
                    DepartureTime = t.EstDepartureTime,
                    ArrivalTime = t.EstArrivalTime,
                    PickupLocation = t.PickupLocation,
                    DropLocation = t.DropoffLocation,
                    Purpose = t.Purpose,
                    Remarks = null,
                    DriverName = t.DispatchDetail.DriverName ?? "",
                    ApprovalStatus = t.ApprovalStatus,
                    TripStatus = t.DispatchDetail.VehicleStatus ?? "Not Started",
                    Requestor = t.RequestorName,
                    Passengers = t.TripTicketPassengers.Select(p => new PassengerDTO
                    {
                        PassengerName = p.PassengerName
                    }).ToList(),
                })
                .ToListAsync();

            return requests;
        }

        public async Task<List<TripTicketResponseDTO>> GetOnTripScheduled()
        {
            var query = dbContext.TripTickets.AsQueryable();
            // await ResolveTransitStatuses();
            var userinfo = await GetUserInfo();

            var requests = await query
                .Include(t => t.TripTicketPassengers)
                .Include(t => t.DispatchDetail)
                .Where(t => t.ApprovalStatus == "In Transit" || t.ApprovalStatus == "Completed" || t.ApprovalStatus == "Ready for Dispatch")
                .Select(t => new TripTicketResponseDTO
                {
                    TicketId = t.TicketId,
                    TicketNumber = t.TicketNumber,
                    RequestDate = DateOnly.FromDateTime(t.DateRequested),
                    RequestDepartment = t.RequestedDepartment ?? "",
                    TripDate = t.DateOfTrip,
                    DepartureTime = t.EstDepartureTime,
                    ArrivalTime = t.EstArrivalTime,
                    PickupLocation = t.PickupLocation,
                    DropLocation = t.DropoffLocation,
                    Purpose = t.Purpose,
                    Remarks = null,
                    DriverName = t.DispatchDetail.DriverName ?? "",
                    ApprovalStatus = t.ApprovalStatus,
                    TripStatus = t.DispatchDetail.VehicleStatus ?? "Not Started",
                    Requestor = t.RequestorName,
                    Passengers = t.TripTicketPassengers.Select(p => new PassengerDTO
                    {
                      PassengerName = p.PassengerName
                    }).ToList(),
                })
                .ToListAsync();

            return requests;
        }


        public async Task<(bool isSuccess, string message)> ProcessApproval(string status, int id)
        {
            try
            {
                var isreqExist = await dbContext.TripTickets.FindAsync(id);
                var httpContext = httpContextAccessor.HttpContext;
                var userIdClaim = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var parsedId))
                {
                    return (false, "Requestor session is not valid.");
                }

                var userinfo = await dbContext.UserTables.Include(u => u.Department).Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == parsedId);

                if (isreqExist == null)
                {
                    return (false, "Trip ticket does not exist.");
                }

                if (userinfo?.Role?.RoleName == "Section Approver" || userinfo?.Role?.RoleName == "Requestor")
                {
                    if (isreqExist.RequestedDepartment != userinfo.Department?.DepartmentName)
                    {
                        return (false, "You are not authorized to approve trips for other sections.");
                    }
                }


                isreqExist.ApproverId = parsedId;
                isreqExist.ApproverName = userinfo?.EmployeeName ?? "Unknown";
                isreqExist.ApprovedAt = DateTime.UtcNow;
                isreqExist.UpdatedAt = DateTime.UtcNow;
                isreqExist.ApprovalStatus = status;

                if (status == "Denied" || status == "Cancelled")
                {
                    var dispatch = await dbContext.DispatchDetails
                        .Include(d => d.Driver)
                        .Include(d => d.Vehicle)
                        .FirstOrDefaultAsync(d => d.TicketId == id);

                    if (dispatch != null)
                    {
                        if (dispatch.Driver != null)
                        {
                            dispatch.Driver.Status = "Active";
                        }
                        if (dispatch.Vehicle != null)
                        {
                            dispatch.Vehicle.Status = "Available";
                        }
                        dispatch.VehicleStatus = "Released";
                    }
                }

                await dbContext.SaveChangesAsync();

                    if (notificationService != null)
                    {
                        var approverRole = userinfo?.Role?.RoleName ?? "";
                        var dept = isreqExist.RequestedDepartment ?? "";
                        var ticketNum = isreqExist.TicketNumber ?? "";

                        if (status == "Cancelled" || status == "Denied")
                        {
                            if (!string.IsNullOrEmpty(isreqExist.RequestorId))
                            {
                                await notificationService.SendToUserAsync(isreqExist.RequestorId, new NotificationMessageDTO
                                {
                                    Title = $"Trip {status}",
                                    Message = $"Your ticket {ticketNum} has been {status.ToLower()} by {userinfo?.EmployeeName ?? "Unknown"}.",
                                    Category = "Approval",
                                    TicketNumber = ticketNum,
                                    Url = "/User/TripSchedule"
                                });
                            }

                            if (status == "Cancelled")
                            {
                                await notificationService.SendToRoleAsync("GA", new NotificationMessageDTO
                                {
                                    Title = "Trip Cancelled",
                                    Message = $"Ticket {ticketNum} has been cancelled by {userinfo?.EmployeeName ?? "Unknown"}.",
                                    Category = "Approval",
                                    TicketNumber = ticketNum,
                                    Url = "/User/TripSchedule"
                                });
                                await notificationService.SendToRoleInDepartmentAsync("Section Approver", dept, new NotificationMessageDTO
                                {
                                    Title = $"Trip {status}",
                                    Message = $"Ticket {ticketNum} has been {status.ToLower()} by {userinfo?.EmployeeName ?? "Unknown"}.",
                                    Category = "Approval",
                                    TicketNumber = ticketNum,
                                    Url = "/User/TripSchedule"
                                });
                            }
                        }
                        else if (approverRole == "Section Approver" || approverRole == "Requestor")
                        {
                            await notificationService.SendToRoleAsync("GA", new NotificationMessageDTO
                            {
                                Title = "Section Head Approved",
                                Message = $"Ticket {ticketNum} approved by Section Head {userinfo?.EmployeeName ?? "Unknown"}.",
                                Category = "Approval",
                                TicketNumber = ticketNum,
                                Url = "/User/TripSchedule"
                            });

                            if (!string.IsNullOrEmpty(isreqExist.RequestorId))
                            {
                                await notificationService.SendToUserAsync(isreqExist.RequestorId, new NotificationMessageDTO
                                {
                                    Title = "Section Head Approved",
                                    Message = $"Your ticket {ticketNum} has been approved by your Section Head.",
                                    Category = "Approval",
                                    TicketNumber = ticketNum,
                                    Url = "/User/TripSchedule"
                                });
                            }
                        }
                        else if (approverRole == "GA")
                        {
                            if (!string.IsNullOrEmpty(isreqExist.RequestorId))
                            {
                                await notificationService.SendToUserAsync(isreqExist.RequestorId, new NotificationMessageDTO
                                {
                                    Title = "GA Approved",
                                    Message = $"Your ticket {ticketNum} has been approved by GA {userinfo?.EmployeeName ?? "Unknown"}.",
                                    Category = "Approval",
                                    TicketNumber = ticketNum,
                                    Url = "/User/TripSchedule"
                                });
                            }

                            await notificationService.SendToRoleInDepartmentAsync("Section Approver", dept, new NotificationMessageDTO
                            {
                                Title = "GA Approved",
                                Message = $"Ticket {ticketNum} has been approved by GA {userinfo?.EmployeeName ?? "Unknown"}.",
                                Category = "Approval",
                                TicketNumber = ticketNum,
                                Url = "/Security/SecurityLogs"
                            });

                            await notificationService.SendToRoleAsync("Security", new NotificationMessageDTO
                            {
                                Title = "GA Approved Trip",
                                Message = $"Ticket {ticketNum} has been approved by GA and is ready for dispatch.",
                                Category = "Approval",
                                TicketNumber = ticketNum,
                                Url = "/Security/SecurityLogs"
                            });
                        }
                    }return (true, $"Trip ticket successfully updated to '{status}'.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating request approval.");
                return (false, $"Error updating request: {ex.Message}");
            }
        }

    

        public async Task<(bool isSuccess, string message)> DeleteRequestApproval(int id)
        {
            try
            {
                var isreqExist = await dbContext.TripTickets.FindAsync(id);
                if (isreqExist == null)
                {
                    return (false, "Trip ticket does not exist.");
                }

                dbContext.TripTickets.Remove(isreqExist);
                await dbContext.SaveChangesAsync();
                return (true, "Trip ticket successfully deleted.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting request.");
                return (false, $"Error deleting request: {ex.Message}");
            }
        }


        public async Task<(bool isSuccess, string message)> CompleteTrip(int ticketId)
        {
            try
            {
                var ticket = await dbContext.TripTickets.FindAsync(ticketId);
                if (ticket == null)
                {
                    return (false, "Trip ticket not found.");
                }

                if (ticket.ApprovalStatus != "On Trip")
                {
                    return (false, $"Trip ticket is not in On Trip status. Current status: {ticket.ApprovalStatus}");
                }

                ticket.ApprovalStatus = "Completed";
                ticket.UpdatedAt = DateTime.Now;

                var dispatch = await dbContext.DispatchDetails
                    .Include(d => d.Driver)
                    .Include(d => d.Vehicle)
                    .FirstOrDefaultAsync(d => d.TicketId == ticketId);

                if (dispatch != null)
                {
                    if (dispatch.Driver != null)
                    {
                        dispatch.Driver.Status = "Available";
                    }
                    if (dispatch.Vehicle != null)
                    {
                        dispatch.Vehicle.Status = "Available";
                    }
                    dispatch.VehicleStatus = "Completed";
                }

                await dbContext.SaveChangesAsync();

                if (notificationService != null)
                {
                    if (!string.IsNullOrEmpty(ticket.RequestorId))
                    {
                        await notificationService.SendToUserAsync(ticket.RequestorId, new NotificationMessageDTO
                        {
                            Title = "Trip Completed",
                            Message = $"Trip ticket {ticket.TicketNumber} has been completed.",
                            Category = "Completion",
                            TicketNumber = ticket.TicketNumber,
                            Url = "/User/TripSchedule"
                        });
                    }
                    await notificationService.SendToRoleAsync("GA", new NotificationMessageDTO
                    {
                        Title = "Trip Completed",
                        Message = $"Trip ticket {ticket.TicketNumber} has been completed.",
                        Category = "Completion",
                        TicketNumber = ticket.TicketNumber,
                        Url = "/User/TripSchedule"
                    });
                    await notificationService.SendToRoleInDepartmentAsync("Section Approver", ticket.RequestedDepartment ?? "", new NotificationMessageDTO
                    {
                        Title = "Trip Completed",
                        Message = $"Trip ticket {ticket.TicketNumber} has been completed.",
                        Category = "Completion",
                        TicketNumber = ticket.TicketNumber,
                        Url = "/User/TripSchedule"
                    });
                }

                return (true, "Trip ticket marked as Completed and resources released.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error completing trip ticket.");
                return (false, $"Error completing trip: {ex.Message}");
            }
        }
       

        public async Task<List<DriverDTO>> GetDriver()
        {
          //  await ResolveTransitStatuses();
            try
            {
                var drivers = await dbContext.Drivers
                    .Where(d => d.Status != "Maintenance" && d.Status != "On Leave" && d.Status != "Suspended" && d.Status != "In Transit" && d.Status != "Assigned")
                    .Select(d => new DriverDTO
                    {
                        Id = d.DriverId,
                        DriverName = d.DriverName,
                        LicenseNumber = d.LicenseNumber ?? "",
                        Status = d.Status
                    })
                    .ToListAsync();

                return drivers;
            }
            catch
            {
                return [];
            }
        }

        public async Task<List<VehicleDTO>> GetVehicle()
        {
           // await ResolveTransitStatuses();
            try
            {
                var vehicle = await dbContext.Vehicles
                    .Where(v => v.Status != "In Transit" && v.Status != "Under Maintenance" && v.Status != "Decommissioned" && v.Status != "Assigned")
                    .Select(v => new VehicleDTO
                    {
                        Id = v.VehicleId,
                        VehicleModel = v.VehicleModel,
                        PlateNumber = v.PlateNumber,
                        Capacity = v.Capacity
                    })
                    .ToListAsync();

                return vehicle;
            }
            catch
            {
                return [];
            }
        }

        public async Task<string> GetPlatenumber(int id)
        {
            try
            {
                var platenumber = await dbContext.Vehicles
                    .Where(v => v.VehicleId == id)
                    .Select(v => v.PlateNumber)
                    .FirstOrDefaultAsync();

                return platenumber ?? "";
            }
            catch
            {
                return "";
            }
        }

        public async Task<int> GetCapacity(int id)
        {
            try
            {
                var capacity = await dbContext.Vehicles
                    .Where(c => c.VehicleId == id)
                    .Select(c => c.Capacity)
                    .FirstOrDefaultAsync();

                return capacity;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<string> GetVehicleStatus(int id)
        {
            try
            {
                var status = await dbContext.Vehicles
                    .Where(v => v.VehicleId == id)
                    .Select(v => v.Status)
                    .FirstOrDefaultAsync();

                return status ?? "";
            }
            catch
            {
                return "";
            }
        }

        public async Task<(bool isSuccess, string message)> SecurityLogs(SecurityLogsDTO securityLogsDTO)
        {
            try
            {
                var ticketInfo = await dbContext.TripTickets
                                        .Include(t => t.SecurityLog)
                                        .Include(t => t.DispatchDetail)
                                        .FirstOrDefaultAsync(t => t.TicketId == securityLogsDTO.TicketId);
                if (ticketInfo == null)
                {
                    return (false, "Trip ticket not found.");
                }

                if(ticketInfo.DispatchDetail == null)
                {
                    return (false, "Dispatch details for this trip ticket not found.");
                }

                var vehicleInfo = await dbContext.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == ticketInfo.DispatchDetail.VehicleId);
                var driverInfo = await dbContext.Drivers.FirstOrDefaultAsync(d => d.DriverId == ticketInfo.DispatchDetail.DriverId);

                var isSecurityLogExist = await dbContext.SecurityLogs.FirstOrDefaultAsync(s => s.TicketId == securityLogsDTO.TicketId);
                
                if (isSecurityLogExist == null)
                {
                    return (false, "Security log for this trip ticket not exists.");
                }

                if(vehicleInfo == null)
                {
                    return (false, "Vehicle information for this trip ticket not found.");
                }

                if(driverInfo == null)
                {
                    return (false, "Driver information for this trip ticket not found.");
                }


                if (ticketInfo.SecurityLog == null)
                {
                    return (false, "Security log for this trip ticket not exists.");
                }

                if (ticketInfo.SecurityLog.ActualDepartureTime == null)
                {
                    return (false, "Departure time has not been logged yet.");
                }

                if (securityLogsDTO.OdometerStart < 0 || securityLogsDTO.OdometerEnd < 0)
                {
                    return (false, "Odometer readings cannot be negative.");
                }

                if(ticketInfo.SecurityLog.ActualDepartureTime >= securityLogsDTO.ArrivalTime)
                {
                    return (false, "Arrival time must be later than departure time.");
                }

                if(securityLogsDTO.OdometerEnd < securityLogsDTO.OdometerStart)
                {
                    return (false, "Odometer end reading cannot be less than start reading.");
                }

                const int maxOdometerValue = 999999; // Example maximum value
                if (securityLogsDTO.OdometerStart > maxOdometerValue || securityLogsDTO.OdometerEnd > maxOdometerValue)
                {
                    return (false, $"Odometer readings cannot exceed {maxOdometerValue}.");
                }

                isSecurityLogExist.ActualArrivalTime = securityLogsDTO.ArrivalTime;
                isSecurityLogExist.KilometerRunFrom = securityLogsDTO.OdometerStart;
                isSecurityLogExist.KilometerRunTo = securityLogsDTO.OdometerEnd;
                isSecurityLogExist.GuardId = securityLogsDTO.GuardId;
                isSecurityLogExist.GuardSignatureName = securityLogsDTO.GuardName;
                isSecurityLogExist.Remarks = securityLogsDTO.Remarks ?? "Completed";
                isSecurityLogExist.LoggedAt = DateTime.UtcNow;

                ticketInfo.DispatchDetail.VehicleStatus = "Completed";
                ticketInfo.ApprovalStatus = "Completed";
                vehicleInfo.Status = "Available";
                driverInfo.Status = "Available";

                await dbContext.SaveChangesAsync();

                if (notificationService != null)
                {
                    if (!string.IsNullOrEmpty(ticketInfo.RequestorId))
                    {
                        await notificationService.SendToUserAsync(ticketInfo.RequestorId, new NotificationMessageDTO
                        {
                            Title = "Trip Completed",
                            Message = $"Trip ticket {ticketInfo.TicketNumber} has been completed and logged by security.",
                            Category = "Security",
                            TicketNumber = ticketInfo.TicketNumber,
                            Url = "/User/TripSchedule"
                        });
                    }
                    await notificationService.SendToRoleAsync("GA", new NotificationMessageDTO
                    {
                        Title = "Trip Completed",
                        Message = $"Trip ticket {ticketInfo.TicketNumber} has been completed and logged by security.",
                        Category = "Security",
                        TicketNumber = ticketInfo.TicketNumber,
                        Url = "/User/TripSchedule"
                    });
                    // Section Head: notify from completed trip
                    await notificationService.SendToRoleInDepartmentAsync("Section Approver", ticketInfo.RequestedDepartment ?? "", new NotificationMessageDTO
                    {
                        Title = "Trip Completed",
                        Message = $"Trip ticket {ticketInfo.TicketNumber} has been completed and logged by security.",
                        Category = "Security",
                        TicketNumber = ticketInfo.TicketNumber,
                        Url = "/User/TripSchedule"
                    });
                }
                return (true, "Security activity logged successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error logging security activity.");
                return (false, $"Error logging security activity: {ex.Message}");
            }
        }


        public async Task<(bool isSuccess, string message)> DispatchConfirm(int ticketId)
        {
            try
            {
                var isreqExist = await dbContext.TripTickets.FindAsync(ticketId);
                var isDispatchExist = await dbContext.DispatchDetails
                                    .Include(d => d.Vehicle)
                                    .Include(d => d.Driver)
                                    .FirstOrDefaultAsync(d => d.TicketId == ticketId);

                if (isreqExist == null)
                {
                    return (false, "Trip ticket does not exist.");
                }

                if (isDispatchExist == null)
                {
                    return (false, "Dispatch details not found for the trip ticket.");
                }

                if (isDispatchExist.Vehicle == null || isDispatchExist.Driver == null)
                    return (false, "Cannot dispatch � vehicle or driver not yet assigned.");

                if (isreqExist.ApprovalStatus != "Ready for Dispatch")
                    return (false, $"Cannot dispatch a ticket with status '{isreqExist.ApprovalStatus}'.");

                var httpContext = httpContextAccessor.HttpContext;
                var userIdClaim = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var parsedId))
                {
                    return (false, "Requestor session is not valid.");
                }



                var userinfo = await dbContext.UserTables.Include(u => u.Department).Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == parsedId);

                if (userinfo == null)
                    return (false, "User account not found.");

                if (isreqExist == null)
                {
                    return (false, "Trip ticket does not exist.");
                }

                if (userinfo?.Role?.RoleName == "Section Approver" || userinfo?.Role?.RoleName == "Requestor")
                {
                    if (isreqExist.RequestedDepartment != userinfo.Department?.DepartmentName)
                    {
                        return (false, "You are not authorized to approve trips for other sections.");
                    }
                }

                isreqExist.UpdatedAt = DateTime.UtcNow;
                isreqExist.ApprovalStatus = "In Transit";

                isDispatchExist.VehicleStatus = "In Transit";
                isDispatchExist.Vehicle.Status = "In Transit";
                isDispatchExist.Driver.Status = "In Transit";

                var securityLog = new SecurityLog
                {
                    TicketId = ticketId,
                    ActualDepartureTime = DateTime.Now,
                    GuardId = parsedId,
                    GuardSignatureName = userinfo?.EmployeeName ?? "Unknown",
                    Remarks = "Trip started and confirmed by security.",
                    LoggedAt = DateTime.UtcNow
                };

                await dbContext.SecurityLogs.AddAsync(securityLog);
                await dbContext.SaveChangesAsync();

                if (notificationService != null)
                {
                    if (!string.IsNullOrEmpty(isreqExist.RequestorId))
                    {
                        await notificationService.SendToUserAsync(isreqExist.RequestorId, new NotificationMessageDTO
                        {
                            Title = "Trip In Transit",
                            Message = $"Trip ticket {isreqExist.TicketNumber} is now In Transit.",
                            Category = "Dispatch",
                            TicketNumber = isreqExist.TicketNumber,
                            Url = "/User/TripSchedule"
                        });
                    }
                    await notificationService.SendToRoleAsync("GA", new NotificationMessageDTO
                    {
                        Title = "Trip In Transit",
                        Message = $"Trip ticket {isreqExist.TicketNumber} is now In Transit.",
                        Category = "Dispatch",
                        TicketNumber = isreqExist.TicketNumber,
                        Url = "/User/TripSchedule"
                    });
                    // Section Head: notify from In Transit trip
                    await notificationService.SendToRoleInDepartmentAsync("Section Approver", isreqExist.RequestedDepartment ?? "", new NotificationMessageDTO
                    {
                        Title = "Trip In Transit",
                        Message = $"Trip ticket {isreqExist.TicketNumber} is now In Transit.",
                        Category = "Dispatch",
                        TicketNumber = isreqExist.TicketNumber,
                        Url = "/User/TripSchedule"
                    });
                }
                return (true, $"Trip ticket successfully updated to 'In Transit'.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating request approval.");
                return (false, $"Error updating request: {ex.Message}");
            }
        }
    }
}

