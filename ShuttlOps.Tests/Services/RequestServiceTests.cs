using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShuttlOps.DTOs;
using ShuttlOps.Models;
using ShuttlOps.Services.MainServices;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace ShuttlOps.Tests.Services
{
    public class RequestServiceTests : IDisposable
    {
        private readonly ShuttlOpsDbContext _dbContext;
        private readonly Mock<ILogger<RequestService>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly RequestService _service;

        public RequestServiceTests()
        {
            var options = new DbContextOptionsBuilder<ShuttlOpsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ShuttlOpsDbContext(options);
            _mockLogger = new Mock<ILogger<RequestService>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            _service = new RequestService(_dbContext, _mockLogger.Object, _mockHttpContextAccessor.Object);
            
            SeedDatabase();
        }
        
        private void SeedDatabase()
        {
            var dept = new Department { DepartmentId = 1, DepartmentName = "IT" };
            var roleRequestor = new RoleTable { RoleId = 1, RoleName = "Requestor" };
            var roleSecurity = new RoleTable { RoleId = 2, RoleName = "Security" };

            _dbContext.Departments.Add(dept);
            _dbContext.RoleTables.AddRange(roleRequestor, roleSecurity);
            
            _dbContext.UserTables.AddRange(
                new UserTable { Id = 1, UserName = "req1", EmployeeName = "Req User", Password = "p", EmailAdd = "a@a.com", RoleId = 1, DepartmentId = 1 },
                new UserTable { Id = 2, UserName = "sec1", EmployeeName = "Sec User", Password = "p", EmailAdd = "b@b.com", RoleId = 2, DepartmentId = 1 }
            );
            
            _dbContext.Vehicles.Add(new Vehicle { VehicleId = 1, PlateNumber = "ABC1234", VehicleModel = "Van", Capacity = 10, Status = "Available", IsActive = true });
            _dbContext.Drivers.Add(new Driver { DriverId = 1, DriverName = "John Doe", Status = "Active", IsActive = true });
            
            _dbContext.SaveChanges();
        }
        
        private void SetupUser(int userId)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            var mockHttpContext = new DefaultHttpContext { User = claimsPrincipal };
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext);
        }

        [Fact]
        public async Task CreateRequest_WhenSecurityRole_ReturnsNoAccessMessage()
        {
            // Arrange
            SetupUser(2); // Security user
            var ticketDTO = new CreateTripTicketDTO
            {
                TripDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                DepartureTime = new TimeOnly(8, 0),
                ArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropLocation = "B",
                Purpose = "Test"
            };

            // Act
            var result = await _service.CreateRequest(ticketDTO);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Requestor lacks access to trip management.");
        }

        [Fact]
        public async Task CreateRequest_WhenNoAvailableVehicles_ReturnsNoShuttlesMessage()
        {
            // Arrange
            SetupUser(1); // Requestor
            var vehicle = await _dbContext.Vehicles.FirstAsync();
            vehicle.Status = "Under Maintenance"; // Make vehicle unavailable
            await _dbContext.SaveChangesAsync();

            var ticketDTO = new CreateTripTicketDTO
            {
                TripDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                DepartureTime = new TimeOnly(8, 0),
                ArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropLocation = "B",
                Purpose = "Test"
            };

            // Act
            var result = await _service.CreateRequest(ticketDTO);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("No available shuttles");
        }

        [Fact]
        public async Task CreateRequest_WhenNoAvailableDrivers_ReturnsNoDriversMessage()
        {
            // Arrange
            SetupUser(1); // Requestor
            var driver = await _dbContext.Drivers.FirstAsync();
            driver.Status = "On Leave"; // Make driver unavailable
            await _dbContext.SaveChangesAsync();

            var ticketDTO = new CreateTripTicketDTO
            {
                TripDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                DepartureTime = new TimeOnly(8, 0),
                ArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropLocation = "B",
                Purpose = "Test"
            };

            // Act
            var result = await _service.CreateRequest(ticketDTO);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("No available drivers");
        }

        [Fact]
        public async Task CreateRequest_WhenOverlappingTripsExhaustResources_ReturnsNoAvailableResourcesMessage()
        {
            // Arrange
            SetupUser(1);
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
            
            // Create a trip that occupies the only driver and vehicle
            var existingTrip = new TripTicket
            {
                TicketNumber = "REQ-1",
                RequestorId = "test",
                RequestorName = "test",
                ApprovalStatus = "Approved",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Test",
                DispatchDetail = new DispatchDetail
                {
                    VehicleId = 1,
                    DriverId = 1,
                    VehicleStatus = "In Transit"
                }
            };
            _dbContext.TripTickets.Add(existingTrip);
            await _dbContext.SaveChangesAsync();

            // Try to book overlapping time
            var ticketDTO = new CreateTripTicketDTO
            {
                TripDate = date,
                DepartureTime = new TimeOnly(9, 0),
                ArrivalTime = new TimeOnly(11, 0),
                PickupLocation = "C",
                DropLocation = "D",
                Purpose = "Overlap"
            };

            // Act
            var result = await _service.CreateRequest(ticketDTO);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("No available shuttles");
        }

        [Fact]
        public async Task CreateRequest_WhenResourcesAvailable_SuccessfullyCreatesTicket()
        {
            // Arrange
            SetupUser(1);
            var ticketDTO = new CreateTripTicketDTO
            {
                TripDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                DepartureTime = new TimeOnly(8, 0),
                ArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropLocation = "B",
                Purpose = "Test"
            };

            // Act
            var result = await _service.CreateRequest(ticketDTO);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Contain("created successfully");
            
            var tickets = await _dbContext.TripTickets.ToListAsync();
            tickets.Should().HaveCount(1);
            tickets[0].Purpose.Should().Be("Test");
        }

        // ============ Trigger 2: In Transit ============


        // ============ Trigger 3: Denied/Cancelled Release ============

        [Fact]
        public async Task ProcessApproval_WhenDenied_ReleasesDriverAndVehicle()
        {
            // Arrange
            SetupUser(1);
            var trip = new TripTicket
            {
                TicketNumber = "REQ-DENY-001",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Pending Section Head",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Deny test",
                RequestedDepartment = "IT"
            };
            _dbContext.TripTickets.Add(trip);

            // Assign driver and vehicle
            var driver = await _dbContext.Drivers.FindAsync(1);
            var vehicle = await _dbContext.Vehicles.FindAsync(1);
            driver!.Status = "Assigned";
            vehicle!.Status = "Assigned";
            await _dbContext.SaveChangesAsync();

            var dispatch = new DispatchDetail
            {
                TicketId = trip.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                VehicleStatus = "Assigned"
            };
            _dbContext.DispatchDetails.Add(dispatch);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.ProcessApproval("Denied", trip.TicketId);

            // Assert
            result.isSuccess.Should().BeTrue();
            var updatedDriver = await _dbContext.Drivers.FindAsync(1);
            var updatedVehicle = await _dbContext.Vehicles.FindAsync(1);
            updatedDriver!.Status.Should().Be("Active");
            updatedVehicle!.Status.Should().Be("Available");
        }

        [Fact]
        public async Task ProcessApproval_WhenCancelled_ReleasesDriverAndVehicle()
        {
            // Arrange
            SetupUser(1);
            var trip = new TripTicket
            {
                TicketNumber = "REQ-CANCEL-001",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Approved",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Cancel test",
                RequestedDepartment = "IT"
            };
            _dbContext.TripTickets.Add(trip);

            var driver = await _dbContext.Drivers.FindAsync(1);
            var vehicle = await _dbContext.Vehicles.FindAsync(1);
            driver!.Status = "Assigned";
            vehicle!.Status = "Assigned";
            await _dbContext.SaveChangesAsync();

            var dispatch = new DispatchDetail
            {
                TicketId = trip.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                VehicleStatus = "Assigned"
            };
            _dbContext.DispatchDetails.Add(dispatch);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.ProcessApproval("Cancelled", trip.TicketId);

            // Assert
            result.isSuccess.Should().BeTrue();
            var updatedDriver = await _dbContext.Drivers.FindAsync(1);
            var updatedVehicle = await _dbContext.Vehicles.FindAsync(1);
            updatedDriver!.Status.Should().Be("Active");
            updatedVehicle!.Status.Should().Be("Available");
        }

        [Fact]
        public async Task ProcessApproval_WhenApproved_DoesNotReleaseDriverAndVehicle()
        {
            // Arrange
            SetupUser(1);
            var trip = new TripTicket
            {
                TicketNumber = "REQ-APPROVE-001",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Pending Section Head",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Approve test",
                RequestedDepartment = "IT"
            };
            _dbContext.TripTickets.Add(trip);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.ProcessApproval("Approved", trip.TicketId);

            // Assert
            result.isSuccess.Should().BeTrue();
            var updatedTrip = await _dbContext.TripTickets.FindAsync(trip.TicketId);
            updatedTrip!.ApprovalStatus.Should().Be("Approved");
        }

        // ============ Trigger 4: Complete Trip ============

        [Fact]
        public async Task CompleteTrip_WhenOnTrip_CompletesAndReleasesResources()
        {
            // Arrange
            var trip = new TripTicket
            {
                TicketNumber = "REQ-COMPLETE-001",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "On Trip",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Complete test"
            };
            _dbContext.TripTickets.Add(trip);

            var driver = await _dbContext.Drivers.FindAsync(1);
            var vehicle = await _dbContext.Vehicles.FindAsync(1);
            driver!.Status = "In Transit";
            vehicle!.Status = "In Transit";
            await _dbContext.SaveChangesAsync();

            var dispatch = new DispatchDetail
            {
                TicketId = trip.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                VehicleStatus = "In Transit"
            };
            _dbContext.DispatchDetails.Add(dispatch);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CompleteTrip(trip.TicketId);

            // Assert
            result.isSuccess.Should().BeTrue();
            var updatedTrip = await _dbContext.TripTickets.FindAsync(trip.TicketId);
            var updatedDriver = await _dbContext.Drivers.FindAsync(1);
            var updatedVehicle = await _dbContext.Vehicles.FindAsync(1);

            updatedTrip!.ApprovalStatus.Should().Be("Completed");
            updatedDriver!.Status.Should().Be("Available");
            updatedVehicle!.Status.Should().Be("Available");
        }

        [Fact]
        public async Task CompleteTrip_WhenNotOnTrip_ReturnsError()
        {
            // Arrange
            var trip = new TripTicket
            {
                TicketNumber = "REQ-NOTRIP-001",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Not on trip"
            };
            _dbContext.TripTickets.Add(trip);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CompleteTrip(trip.TicketId);

            // Assert
            result.isSuccess.Should().BeFalse();
            result.message.Should().Contain("not in On Trip status");
        }

        [Fact]
        public async Task CompleteTrip_WhenTicketNotFound_ReturnsError()
        {
            // Act
            var result = await _service.CompleteTrip(999);

            // Assert
            result.isSuccess.Should().BeFalse();
            result.message.Should().Contain("not found");
        }

        // ============ Availability Filters with Assigned Status ============

        [Fact]
        public async Task GetDriver_ExcludesInTransit_ShowsAssignedDrivers()
        {
            // Arrange
            var driver2 = new Driver { DriverId = 2, DriverName = "Jane Doe", Status = "In Transit", IsActive = true };
            var driver3 = new Driver { DriverId = 3, DriverName = "Bob Smith", Status = "Assigned", IsActive = true };
            _dbContext.Drivers.AddRange(driver2, driver3);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetDriver();

            // Assert
            result.Should().HaveCount(1); // John (Active) only - Assigned and In Transit excluded
            result.Should().Contain(d => d.DriverName == "John Doe");
            
            result.Should().NotContain(d => d.DriverName == "Jane Doe");
        }

        [Fact]
        public async Task GetVehicle_ExcludesInTransit_ShowsAssignedVehicles()
        {
            // Arrange
            var vehicle2 = new Vehicle { VehicleId = 2, PlateNumber = "XYZ5678", VehicleModel = "Bus", Capacity = 20, Status = "In Transit", IsActive = true };
            var vehicle3 = new Vehicle { VehicleId = 3, PlateNumber = "DEF9012", VehicleModel = "Car", Capacity = 5, Status = "Assigned", IsActive = true };
            _dbContext.Vehicles.AddRange(vehicle2, vehicle3);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetVehicle();

            // Assert
            result.Should().HaveCount(1); // ABC1234 (Available) only - Assigned and In Transit excluded
            result.Should().Contain(v => v.PlateNumber == "ABC1234");
            
            result.Should().NotContain(v => v.PlateNumber == "XYZ5678");
        }


        // ============================================================
        // DispatchConfirm Tests
        // ============================================================

        [Fact]
        public async Task DispatchConfirm_WhenValid_CreatesSecurityLogAndSetsInTransit()
        {
            // Arrange
            var trip = new TripTicket
            {
                TicketNumber = "REQ-DC-001",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Dispatch test"
            };
            _dbContext.TripTickets.Add(trip);
            await _dbContext.SaveChangesAsync();

            var dispatch = new DispatchDetail
            {
                TicketId = trip.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                VehicleStatus = "Assigned"
            };
            _dbContext.DispatchDetails.Add(dispatch);
            await _dbContext.SaveChangesAsync();

            SetupUser(2); // Security user

            // Act
            var (isSuccess, message) = await _service.DispatchConfirm(trip.TicketId);

            // Assert
            isSuccess.Should().BeTrue();
            message.Should().Contain("In Transit");

            var updatedTicket = await _dbContext.TripTickets.FindAsync(trip.TicketId);
            updatedTicket!.ApprovalStatus.Should().Be("In Transit");

            var updatedDriver = await _dbContext.Drivers.FindAsync(1);
            updatedDriver!.Status.Should().Be("In Transit");

            var updatedVehicle = await _dbContext.Vehicles.FindAsync(1);
            updatedVehicle!.Status.Should().Be("In Transit");

            var securityLog = await _dbContext.SecurityLogs.FirstOrDefaultAsync(s => s.TicketId == trip.TicketId);
            securityLog.Should().NotBeNull();
            securityLog!.ActualDepartureTime.Should().NotBeNull();
        }

        [Fact]
        public async Task DispatchConfirm_WhenTicketNotFound_ReturnsError()
        {
            // Arrange
            SetupUser(2);

            // Act
            var (isSuccess, message) = await _service.DispatchConfirm(9999);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("does not exist");
        }

        [Fact]
        public async Task DispatchConfirm_WhenNoDispatchDetails_ReturnsError()
        {
            // Arrange
            var trip = new TripTicket
            {
                TicketNumber = "REQ-DC-NODISP",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "No dispatch"
            };
            _dbContext.TripTickets.Add(trip);
            await _dbContext.SaveChangesAsync();

            SetupUser(2);

            // Act
            var (isSuccess, message) = await _service.DispatchConfirm(trip.TicketId);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("Dispatch details not found");
        }

        [Fact]
        public async Task DispatchConfirm_WhenVehicleNotAssigned_ReturnsError()
        {
            // Arrange
            var trip = new TripTicket
            {
                TicketNumber = "REQ-DC-NOVEH",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "No vehicle assigned"
            };
            _dbContext.TripTickets.Add(trip);
            await _dbContext.SaveChangesAsync();

            var dispatch = new DispatchDetail
            {
                TicketId = trip.TicketId,
                VehicleStatus = "Assigned"
            };
            _dbContext.DispatchDetails.Add(dispatch);
            await _dbContext.SaveChangesAsync();

            SetupUser(2);

            // Act
            var (isSuccess, message) = await _service.DispatchConfirm(trip.TicketId);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("vehicle or driver not yet assigned");
        }

        [Fact]
        public async Task DispatchConfirm_WhenWrongStatus_ReturnsError()
        {
            // Arrange
            var trip = new TripTicket
            {
                TicketNumber = "REQ-DC-WRONGSTATUS",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Approved",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Wrong status"
            };
            _dbContext.TripTickets.Add(trip);
            await _dbContext.SaveChangesAsync();

            var dispatch = new DispatchDetail
            {
                TicketId = trip.TicketId,
                DriverId = 1,
                VehicleId = 1,
                VehicleStatus = "Assigned"
            };
            _dbContext.DispatchDetails.Add(dispatch);
            await _dbContext.SaveChangesAsync();

            SetupUser(2);

            // Act
            var (isSuccess, message) = await _service.DispatchConfirm(trip.TicketId);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("Cannot dispatch a ticket with status");
        }

        [Fact]
        public async Task DispatchConfirm_WhenUserNotFound_ReturnsError()
        {
            // Arrange
            var trip = new TripTicket
            {
                TicketNumber = "REQ-DC-NOUSER",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "No user"
            };
            _dbContext.TripTickets.Add(trip);
            await _dbContext.SaveChangesAsync();

            var dispatch = new DispatchDetail
            {
                TicketId = trip.TicketId,
                DriverId = 1,
                VehicleId = 1,
                VehicleStatus = "Assigned"
            };
            _dbContext.DispatchDetails.Add(dispatch);
            await _dbContext.SaveChangesAsync();

            SetupUser(999); // Non-existent user

            // Act
            var (isSuccess, message) = await _service.DispatchConfirm(trip.TicketId);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("User account not found");
        }

        // ============================================================
        // SecurityLogs Tests
        // ============================================================

        [Fact]
        public async Task SecurityLogs_WhenValidCompletion_SetsCompletedAndReleasesResources()
        {
            // Arrange
            var trip = new TripTicket
            {
                TicketNumber = "REQ-SEC-001",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "In Transit",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Security test"
            };
            _dbContext.TripTickets.Add(trip);
            await _dbContext.SaveChangesAsync();

            var dispatch = new DispatchDetail
            {
                TicketId = trip.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                VehicleStatus = "In Transit"
            };
            _dbContext.DispatchDetails.Add(dispatch);

            var securityLog = new SecurityLog
            {
                TicketId = trip.TicketId,
                ActualDepartureTime = DateTime.Now.AddHours(-2),
                GuardSignatureName = "Guard 1"
            };
            _dbContext.SecurityLogs.Add(securityLog);

            var driver = await _dbContext.Drivers.FindAsync(1);
            driver!.Status = "In Transit";
            var vehicle = await _dbContext.Vehicles.FindAsync(1);
            vehicle!.Status = "In Transit";
            await _dbContext.SaveChangesAsync();

            var dto = new SecurityLogsDTO
            {
                TicketId = trip.TicketId,
                DepartureTime = DateTime.Now.AddHours(-2),
                ArrivalTime = DateTime.Now.AddMinutes(-30),
                OdometerStart = 1000,
                OdometerEnd = 1050,
                GuardId = 2,
                GuardName = "Guard 1",
                Remarks = "Trip completed safely"
            };

            // Act
            var (isSuccess, message) = await _service.SecurityLogs(dto);

            // Assert
            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");

            var updatedTrip = await _dbContext.TripTickets.FindAsync(trip.TicketId);
            updatedTrip!.ApprovalStatus.Should().Be("Completed");

            var updatedDriver = await _dbContext.Drivers.FindAsync(1);
            updatedDriver!.Status.Should().Be("Available");

            var updatedVehicle = await _dbContext.Vehicles.FindAsync(1);
            updatedVehicle!.Status.Should().Be("Available");

            var updatedLog = await _dbContext.SecurityLogs.FirstOrDefaultAsync(s => s.TicketId == trip.TicketId);
            updatedLog!.ActualArrivalTime.Should().NotBeNull();
            updatedLog.KilometerRunFrom.Should().Be(1000);
            updatedLog.KilometerRunTo.Should().Be(1050);
        }

        [Fact]
        public async Task SecurityLogs_WhenTicketNotFound_ReturnsError()
        {
            // Arrange
            var dto = new SecurityLogsDTO
            {
                TicketId = 9999,
                DepartureTime = DateTime.Now,
                ArrivalTime = DateTime.Now.AddHours(1),
                OdometerStart = 1000,
                OdometerEnd = 1050,
                GuardId = 2,
                GuardName = "Guard",
                Remarks = "Test"
            };

            // Act
            var (isSuccess, message) = await _service.SecurityLogs(dto);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("not found");
        }

        [Fact]
        public async Task SecurityLogs_WhenNoSecurityLogExists_ReturnsError()
        {
            // Arrange
            var trip = new TripTicket
            {
                TicketNumber = "REQ-SEC-NOLOG",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "In Transit",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "No log"
            };
            _dbContext.TripTickets.Add(trip);
            await _dbContext.SaveChangesAsync();

            var dispatch = new DispatchDetail
            {
                TicketId = trip.TicketId,
                DriverId = 1,
                VehicleId = 1,
                VehicleStatus = "In Transit"
            };
            _dbContext.DispatchDetails.Add(dispatch);
            await _dbContext.SaveChangesAsync();

            var dto = new SecurityLogsDTO
            {
                TicketId = trip.TicketId,
                DepartureTime = DateTime.Now,
                ArrivalTime = DateTime.Now.AddHours(1),
                OdometerStart = 1000,
                OdometerEnd = 1050,
                GuardId = 2,
                GuardName = "Guard",
                Remarks = "Test"
            };

            // Act
            var (isSuccess, message) = await _service.SecurityLogs(dto);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("Security log");
        }

        [Fact]
        public async Task SecurityLogs_WhenOdometerStartNegative_ReturnsError()
        {
            // Arrange
            var trip = new TripTicket
            {
                TicketNumber = "REQ-SEC-ODONEG",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "In Transit",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Negative odometer"
            };
            _dbContext.TripTickets.Add(trip);
            await _dbContext.SaveChangesAsync();

            var dispatch = new DispatchDetail
            {
                TicketId = trip.TicketId,
                DriverId = 1,
                VehicleId = 1,
                VehicleStatus = "In Transit"
            };
            _dbContext.DispatchDetails.Add(dispatch);

            var securityLog = new SecurityLog
            {
                TicketId = trip.TicketId,
                ActualDepartureTime = DateTime.Now.AddHours(-2),
                GuardSignatureName = "Guard"
            };
            _dbContext.SecurityLogs.Add(securityLog);
            await _dbContext.SaveChangesAsync();

            var dto = new SecurityLogsDTO
            {
                TicketId = trip.TicketId,
                DepartureTime = DateTime.Now,
                ArrivalTime = DateTime.Now.AddHours(1),
                OdometerStart = -100,
                OdometerEnd = 1050,
                GuardId = 2,
                GuardName = "Guard",
                Remarks = "Negative start"
            };

            // Act
            var (isSuccess, message) = await _service.SecurityLogs(dto);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("negative");
        }

        [Fact]
        public async Task SecurityLogs_WhenOdometerEndLessThanStart_ReturnsError()
        {
            // Arrange
            var trip = new TripTicket
            {
                TicketNumber = "REQ-SEC-ODOREV",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "In Transit",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Reversed odometer"
            };
            _dbContext.TripTickets.Add(trip);
            await _dbContext.SaveChangesAsync();

            var dispatch = new DispatchDetail
            {
                TicketId = trip.TicketId,
                DriverId = 1,
                VehicleId = 1,
                VehicleStatus = "In Transit"
            };
            _dbContext.DispatchDetails.Add(dispatch);

            var securityLog = new SecurityLog
            {
                TicketId = trip.TicketId,
                ActualDepartureTime = DateTime.Now.AddHours(-2),
                GuardSignatureName = "Guard"
            };
            _dbContext.SecurityLogs.Add(securityLog);
            await _dbContext.SaveChangesAsync();

            var dto = new SecurityLogsDTO
            {
                TicketId = trip.TicketId,
                DepartureTime = DateTime.Now,
                ArrivalTime = DateTime.Now.AddHours(1),
                OdometerStart = 2000,
                OdometerEnd = 1000,
                GuardId = 2,
                GuardName = "Guard",
                Remarks = "Reversed"
            };

            // Act
            var (isSuccess, message) = await _service.SecurityLogs(dto);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("less than start");
        }

        [Fact]
        public async Task SecurityLogs_WhenArrivalNotAfterDeparture_ReturnsError()
        {
            // Arrange
            var trip = new TripTicket
            {
                TicketNumber = "REQ-SEC-ARRIVE",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "In Transit",
                DateOfTrip = DateOnly.FromDateTime(DateTime.Today),
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Arrival before departure"
            };
            _dbContext.TripTickets.Add(trip);
            await _dbContext.SaveChangesAsync();

            var dispatch = new DispatchDetail
            {
                TicketId = trip.TicketId,
                DriverId = 1,
                VehicleId = 1,
                VehicleStatus = "In Transit"
            };
            _dbContext.DispatchDetails.Add(dispatch);

            var securityLog = new SecurityLog
            {
                TicketId = trip.TicketId,
                ActualDepartureTime = DateTime.Now.AddHours(-1),
                GuardSignatureName = "Guard"
            };
            _dbContext.SecurityLogs.Add(securityLog);
            await _dbContext.SaveChangesAsync();

            var dto = new SecurityLogsDTO
            {
                TicketId = trip.TicketId,
                DepartureTime = DateTime.Now,
                ArrivalTime = DateTime.Now.AddHours(-2),
                OdometerStart = 1000,
                OdometerEnd = 1050,
                GuardId = 2,
                GuardName = "Guard",
                Remarks = "Arrived before departed"
            };

            // Act
            var (isSuccess, message) = await _service.SecurityLogs(dto);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("Arrival time must be later");
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
