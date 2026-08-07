using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ShuttlOps.DTOs;
using ShuttlOps.Models;
using System.Security.Claims;

namespace ShuttlOps.Services
{
    [Authorize]
    public class RequestService(
        ShuttlOpsDbContext dbContext,
        ILogger<RequestService> logger,
        IHttpContextAccessor httpContextAccessor
    ) : IRequestService
    {

        public async Task<(bool IsSuccess, string Message)> CreateRequest(CreateTripTicketDTO ticketDTO)
        {
            try
            {
                if (ticketDTO == null)
                {
                    return (false, "Invalid request payload.");
                }

                int seq = await GenerateTicketId();
                string ticketNumber = $"REQ-{DateTime.Now:yyyyMMdd}-{seq:D4}";
                var httpContext = httpContextAccessor.HttpContext;
                var userIdClaim = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return (false, "Requestor id is not valid.");
                }
                var userinfo = await dbContext.UserTables.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == int.Parse(userIdClaim));

                if (userinfo == null)
                {
                    return (false, $"Requestor info: {userIdClaim} is null.");
                }
     
                var ticket = new TripTicket
                {
                    TicketNumber = ticketNumber,
                    RequestorId = userinfo.UserName,
                    RequestorName = userinfo?.EmployeeName ?? "Unknown",
                    ApprovalStatus = "Pending",
                    DateRequested = ticketDTO.RequestDate,
                    DateOfTrip = ticketDTO.TripDate,
                    RequestedDepartment = userinfo?.Department?.DepartmentName ?? "Unknown",
                    EstDepartureTime = ticketDTO.DepartureTime,
                    EstArrivalTime = ticketDTO.ArrivalTime,
                    PickupLocation = ticketDTO.PickupLocation,
                    DropoffLocation = ticketDTO.DropLocation,
                    Purpose = ticketDTO.Purpose,
                    CreatedAt = DateTime.Now,
                    TripTicketPassengers = [.. (ticketDTO.Passengers ?? new List<PassengerDTO>())
                        .Select(p => new TripTicketPassenger
                        {
                            PassengerName = p.PassengerName
                        })]
                };

                await dbContext.TripTickets.AddAsync(ticket);
                await dbContext.SaveChangesAsync();
                return (true, $"Request created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating request");
                return (false, $"Error creating request: {ex.Message}");
            }
        }

        public async Task<int> GenerateTicketId()
        {
            var result = await dbContext.Database
             .SqlQueryRaw<int>("EXEC [dbo].[GetNextRequestId]")
             .ToListAsync();
            return result.FirstOrDefault();
        }

        public async Task<List<TripTicketResponseDTO>> GetAllRequests()
        {
            var requests = await dbContext.TripTickets
                .Include(t => t.TripTicketPassengers)
                .Select(t => new TripTicketResponseDTO
                {
                    TicketId = t.TicketId,
                    TicketNumber = t.TicketNumber,
                    RequestDate = DateOnly.FromDateTime(t.DateRequested),
                    TripDate = t.DateOfTrip,
                    DepartureTime = t.EstDepartureTime,
                    ArrivalTime = t.EstArrivalTime,
                    PickupLocation = t.PickupLocation,
                    DropLocation = t.DropoffLocation,
                    Purpose = t.Purpose,
                    Remarks = null, // Assuming Remarks is not stored in TripTicket, adjust if needed
                    ApprovalStatus = t.ApprovalStatus,
                    Passengers = t.TripTicketPassengers.Select(p => new PassengerDTO
                    {
                        PassengerName = p.PassengerName
                    }).ToList()
                })
                .ToListAsync();
            return requests;
        }
    }
}
