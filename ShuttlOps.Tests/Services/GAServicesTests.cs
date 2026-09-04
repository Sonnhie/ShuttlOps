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
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace ShuttlOps.Tests.Services
{
    public class GAServicesTests : IDisposable
    {
        private readonly ShuttlOpsDbContext _dbContext;
        private readonly Mock<ILogger<GAServices>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly GAServices _service;

        public GAServicesTests()
        {
            var options = new DbContextOptionsBuilder<ShuttlOpsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ShuttlOpsDbContext(options);
            _mockLogger = new Mock<ILogger<GAServices>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            _service = new GAServices(_dbContext, _mockLogger.Object, _mockHttpContextAccessor.Object);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var dept = new Department { DepartmentId = 1, DepartmentName = "IT" };
            var roleGA = new RoleTable { RoleId = 3, RoleName = "GA" };

            _dbContext.Departments.Add(dept);
            _dbContext.RoleTables.Add(roleGA);

            _dbContext.UserTables.Add(
                new UserTable { Id = 10, UserName = "ga1", EmployeeName = "GA User", Password = "p", EmailAdd = "ga@a.com", RoleId = 3, DepartmentId = 1 }
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

        private TripTicket CreateTestTicket(string status = "Ready for Dispatch", DateOnly? tripDate = null, TimeOnly? departure = null, TimeOnly? arrival = null)
        {
            var ticket = new TripTicket
            {
                TicketNumber = $"REQ-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                RequestorId = "req1",
                RequestorName = "Requestor",
                ApprovalStatus = status,
                DateOfTrip = tripDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                EstDepartureTime = departure ?? new TimeOnly(8, 0),
                EstArrivalTime = arrival ?? new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Test"
            };
            _dbContext.TripTickets.Add(ticket);
            _dbContext.SaveChanges();
            return ticket;
        }

        // ============================================================
        // AssignDriver Tests
        // ============================================================

        [Fact]
        public async Task AssignDriver_WhenValidAssignment_CreatesDispatchRecord()
        {
            // Arrange
            SetupUser(10);
            var ticket = CreateTestTicket();

            var dispatchDto = new DispatchDto
            {
                TicketId = ticket.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                Remarks = "Assigned for morning trip",
                Status = "Ready for Dispatch"
            };

            // Act
            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            // Assert
            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");

            var dispatch = await _dbContext.DispatchDetails.FirstOrDefaultAsync(d => d.TicketId == ticket.TicketId);
            dispatch.Should().NotBeNull();
            dispatch!.DriverId.Should().Be(1);
            dispatch.VehicleId.Should().Be(1);

            var driver = await _dbContext.Drivers.FindAsync(1);
            driver!.Status.Should().Be("Assigned");

            var vehicle = await _dbContext.Vehicles.FindAsync(1);
            vehicle!.Status.Should().Be("Assigned");

            var updatedTicket = await _dbContext.TripTickets.FindAsync(ticket.TicketId);
            updatedTicket!.ApprovalStatus.Should().Be("Ready for Dispatch");
        }

        [Fact]
        public async Task AssignDriver_WhenDriverIsInTransit_ReturnsError()
        {
            // Arrange
            SetupUser(10);
            var ticket = CreateTestTicket();

            var driver = await _dbContext.Drivers.FindAsync(1);
            driver!.Status = "In Transit";
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = ticket.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                Remarks = "Should fail",
                Status = "Ready for Dispatch"
            };

            // Act
            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("In Transit");

            var dispatch = await _dbContext.DispatchDetails.FirstOrDefaultAsync(d => d.TicketId == ticket.TicketId);
            dispatch.Should().BeNull();
        }

        [Fact]
        public async Task AssignDriver_WhenVehicleIsInTransit_ReturnsError()
        {
            // Arrange
            SetupUser(10);
            var ticket = CreateTestTicket();

            var vehicle = await _dbContext.Vehicles.FindAsync(1);
            vehicle!.Status = "In Transit";
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = ticket.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                Remarks = "Should fail",
                Status = "Ready for Dispatch"
            };

            // Act
            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("In Transit");

            var dispatch = await _dbContext.DispatchDetails.FirstOrDefaultAsync(d => d.TicketId == ticket.TicketId);
            dispatch.Should().BeNull();
        }

        [Fact]
        public async Task AssignDriver_WhenTicketAlreadyHasDispatch_ReturnsError()
        {
            // Arrange
            SetupUser(10);
            var ticket = CreateTestTicket();

            // Create existing dispatch
            var existingDispatch = new DispatchDetail
            {
                TicketId = ticket.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                VehicleStatus = "Assigned",
                ConfirmedAt = DateTime.UtcNow
            };
            _dbContext.DispatchDetails.Add(existingDispatch);
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = ticket.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                Remarks = "Duplicate",
                Status = "Ready for Dispatch"
            };

            // Act
            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("already has an assigned driver");

            var dispatchCount = await _dbContext.DispatchDetails.CountAsync(d => d.TicketId == ticket.TicketId);
            dispatchCount.Should().Be(1);
        }

        [Fact]
        public async Task AssignDriver_WhenDriverHasTimeConflict_ReturnsError()
        {
            // Arrange
            SetupUser(10);
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            // Create existing ticket with driver assigned in overlapping time
            var existingTicket = new TripTicket
            {
                TicketNumber = "REQ-EXISTING-001",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(11, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Existing trip"
            };
            _dbContext.TripTickets.Add(existingTicket);
            await _dbContext.SaveChangesAsync();

            var existingDispatch = new DispatchDetail
            {
                TicketId = existingTicket.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                VehicleStatus = "Assigned"
            };
            _dbContext.DispatchDetails.Add(existingDispatch);
            await _dbContext.SaveChangesAsync();

            // New ticket overlapping with existing
            var newTicket = new TripTicket
            {
                TicketNumber = "REQ-NEW-001",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Approved",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(9, 0),  // Overlaps: 9:00 < 11:00
                EstArrivalTime = new TimeOnly(12, 0),     // Overlaps: 12:00 > 8:00
                PickupLocation = "C",
                DropoffLocation = "D",
                Purpose = "New trip"
            };
            _dbContext.TripTickets.Add(newTicket);
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = newTicket.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                Remarks = "Should fail - time conflict",
                Status = "Ready for Dispatch"
            };

            // Act
            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("already assigned");
        }

        [Fact]
        public async Task AssignDriver_WhenVehicleHasTimeConflict_ReturnsError()
        {
            // Arrange
            SetupUser(10);
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            // Create existing ticket with vehicle assigned in overlapping time
            var existingTicket = new TripTicket
            {
                TicketNumber = "REQ-V-EXISTING",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "In Transit",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(13, 0),
                EstArrivalTime = new TimeOnly(16, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Existing trip"
            };
            _dbContext.TripTickets.Add(existingTicket);
            await _dbContext.SaveChangesAsync();

            var existingDispatch = new DispatchDetail
            {
                TicketId = existingTicket.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                VehicleStatus = "In Transit"
            };
            _dbContext.DispatchDetails.Add(existingDispatch);
            await _dbContext.SaveChangesAsync();

            // New ticket overlapping
            var newTicket = new TripTicket
            {
                TicketNumber = "REQ-V-NEW",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Approved",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(14, 0),
                EstArrivalTime = new TimeOnly(17, 0),
                PickupLocation = "C",
                DropoffLocation = "D",
                Purpose = "New trip"
            };
            _dbContext.TripTickets.Add(newTicket);
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = newTicket.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                Remarks = "Should fail - vehicle conflict",
                Status = "Ready for Dispatch"
            };

            // Act
            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("already assigned");
        }

        [Fact]
        public async Task AssignDriver_WhenNoTimeConflict_Succeeds()
        {
            // Arrange
            SetupUser(10);
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            // Existing ticket: 8:00 - 10:00
            var existingTicket = new TripTicket
            {
                TicketNumber = "REQ-NOCON-EXISTING",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Morning trip"
            };
            _dbContext.TripTickets.Add(existingTicket);
            await _dbContext.SaveChangesAsync();

            var existingDispatch = new DispatchDetail
            {
                TicketId = existingTicket.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                VehicleStatus = "Assigned"
            };
            _dbContext.DispatchDetails.Add(existingDispatch);
            await _dbContext.SaveChangesAsync();

            // New ticket: 11:00 - 13:00 (no overlap)
            var newTicket = new TripTicket
            {
                TicketNumber = "REQ-NOCON-NEW",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Approved",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(11, 0),
                EstArrivalTime = new TimeOnly(13, 0),
                PickupLocation = "C",
                DropoffLocation = "D",
                Purpose = "Afternoon trip"
            };
            _dbContext.TripTickets.Add(newTicket);
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = newTicket.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                Remarks = "No conflict",
                Status = "Ready for Dispatch"
            };

            // Act
            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            // Assert
            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");
        }

        [Fact]
        public async Task AssignDriver_WhenTicketDoesNotExist_ReturnsError()
        {
            // Arrange
            SetupUser(10);

            var dispatchDto = new DispatchDto
            {
                TicketId = 9999,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                Remarks = "No ticket",
                Status = "Ready for Dispatch"
            };

            // Act
            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("not exist");
        }

        [Fact]
        public async Task AssignDriver_WhenDriverDoesNotExist_ReturnsError()
        {
            // Arrange
            SetupUser(10);
            var ticket = CreateTestTicket();

            var dispatchDto = new DispatchDto
            {
                TicketId = ticket.TicketId,
                DriverId = 9999,
                VehicleId = 1,
                DriverName = "Ghost Driver",
                PlateNumber = "ABC1234",
                Remarks = "No driver",
                Status = "Ready for Dispatch"
            };

            // Act
            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("Driver does not exist");
        }

        [Fact]
        public async Task AssignDriver_WhenVehicleDoesNotExist_ReturnsError()
        {
            // Arrange
            SetupUser(10);
            var ticket = CreateTestTicket();

            var dispatchDto = new DispatchDto
            {
                TicketId = ticket.TicketId,
                DriverId = 1,
                VehicleId = 9999,
                DriverName = "John Doe",
                PlateNumber = "GHOST",
                Remarks = "No vehicle",
                Status = "Ready for Dispatch"
            };

            // Act
            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            // Assert
            isSuccess.Should().BeFalse();
            message.Should().Contain("Vehicle does not exist");
        }

        [Fact]
        public async Task AssignDriver_WhenCancelledTicketConflict_Ignored()
        {
            // Arrange
            SetupUser(10);
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            // Cancelled ticket with same driver/vehicle in overlapping time
            var cancelledTicket = new TripTicket
            {
                TicketNumber = "REQ-CANCELLED",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Cancelled",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(11, 0),
                PickupLocation = "A",
                DropoffLocation = "B",
                Purpose = "Cancelled trip"
            };
            _dbContext.TripTickets.Add(cancelledTicket);
            await _dbContext.SaveChangesAsync();

            var cancelledDispatch = new DispatchDetail
            {
                TicketId = cancelledTicket.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                VehicleStatus = "Assigned"
            };
            _dbContext.DispatchDetails.Add(cancelledDispatch);
            await _dbContext.SaveChangesAsync();

            // New ticket overlapping with cancelled
            var newTicket = new TripTicket
            {
                TicketNumber = "REQ-AFTER-CANCEL",
                RequestorId = "req1",
                RequestorName = "Req User",
                ApprovalStatus = "Approved",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(9, 0),
                EstArrivalTime = new TimeOnly(12, 0),
                PickupLocation = "C",
                DropoffLocation = "D",
                Purpose = "After cancel"
            };
            _dbContext.TripTickets.Add(newTicket);
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = newTicket.TicketId,
                DriverId = 1,
                VehicleId = 1,
                DriverName = "John Doe",
                PlateNumber = "ABC1234",
                Remarks = "Should succeed",
                Status = "Ready for Dispatch"
            };

            // Act
            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            // Assert
            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");
        }

        // ============================================================
        // Driver/Vehicle Sharing & Capacity Maximization Tests
        // ============================================================

        [Fact]
        public async Task AssignDriver_SameDriverOnBackToBackTrips_SameDay_Succeeds()
        {
            SetupUser(10);
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            var trip1 = new TripTicket
            {
                TicketNumber = "REQ-BTB-001",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A", DropoffLocation = "B",
                Purpose = "Morning"
            };
            _dbContext.TripTickets.Add(trip1);
            await _dbContext.SaveChangesAsync();

            _dbContext.DispatchDetails.Add(new DispatchDetail
            {
                TicketId = trip1.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234",
                VehicleStatus = "Assigned"
            });
            await _dbContext.SaveChangesAsync();

            var trip2 = new TripTicket
            {
                TicketNumber = "REQ-BTB-002",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Approved",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(10, 0),
                EstArrivalTime = new TimeOnly(12, 0),
                PickupLocation = "C", DropoffLocation = "D",
                Purpose = "Midday"
            };
            _dbContext.TripTickets.Add(trip2);
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = trip2.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234",
                Remarks = "Back-to-back", Status = "Ready for Dispatch"
            };

            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");
            var dispatchCount = await _dbContext.DispatchDetails.CountAsync(d => d.DriverId == 1);
            dispatchCount.Should().Be(2);
        }

        [Fact]
        public async Task AssignDriver_SameDriverThreeNonOverlappingTrips_SameDay_Succeeds()
        {
            SetupUser(10);
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            var trip1 = new TripTicket
            {
                TicketNumber = "REQ-MAX-001",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(6, 0),
                EstArrivalTime = new TimeOnly(8, 0),
                PickupLocation = "A", DropoffLocation = "B",
                Purpose = "Early"
            };
            _dbContext.TripTickets.Add(trip1);
            await _dbContext.SaveChangesAsync();
            _dbContext.DispatchDetails.Add(new DispatchDetail
            {
                TicketId = trip1.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234",
                VehicleStatus = "Assigned"
            });
            await _dbContext.SaveChangesAsync();

            var trip2 = new TripTicket
            {
                TicketNumber = "REQ-MAX-002",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "C", DropoffLocation = "D",
                Purpose = "Mid"
            };
            _dbContext.TripTickets.Add(trip2);
            await _dbContext.SaveChangesAsync();
            _dbContext.DispatchDetails.Add(new DispatchDetail
            {
                TicketId = trip2.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234",
                VehicleStatus = "Assigned"
            });
            await _dbContext.SaveChangesAsync();

            var trip3 = new TripTicket
            {
                TicketNumber = "REQ-MAX-003",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Approved",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(10, 0),
                EstArrivalTime = new TimeOnly(12, 0),
                PickupLocation = "E", DropoffLocation = "F",
                Purpose = "Late"
            };
            _dbContext.TripTickets.Add(trip3);
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = trip3.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234",
                Remarks = "Third trip", Status = "Ready for Dispatch"
            };

            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");
            var dispatchCount = await _dbContext.DispatchDetails.CountAsync(d => d.DriverId == 1);
            dispatchCount.Should().Be(3);
        }

        [Fact]
        public async Task AssignDriver_SameDriverWithOneMinuteOverlap_Fails()
        {
            SetupUser(10);
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            var trip1 = new TripTicket
            {
                TicketNumber = "REQ-MIN-001",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A", DropoffLocation = "B",
                Purpose = "First"
            };
            _dbContext.TripTickets.Add(trip1);
            await _dbContext.SaveChangesAsync();
            _dbContext.DispatchDetails.Add(new DispatchDetail
            {
                TicketId = trip1.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234",
                VehicleStatus = "Assigned"
            });
            await _dbContext.SaveChangesAsync();

            var trip2 = new TripTicket
            {
                TicketNumber = "REQ-MIN-002",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Approved",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(9, 59),
                EstArrivalTime = new TimeOnly(12, 0),
                PickupLocation = "C", DropoffLocation = "D",
                Purpose = "Overlaps by 1 min"
            };
            _dbContext.TripTickets.Add(trip2);
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = trip2.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234",
                Remarks = "Should fail", Status = "Ready for Dispatch"
            };

            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            isSuccess.Should().BeFalse();
            message.Should().Contain("already assigned");
        }

        [Fact]
        public async Task AssignDriver_SameVehicleOnNonOverlappingTrips_SameDay_Succeeds()
        {
            SetupUser(10);
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            var trip1 = new TripTicket
            {
                TicketNumber = "REQ-VMX-001",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A", DropoffLocation = "B",
                Purpose = "Trip A"
            };
            _dbContext.TripTickets.Add(trip1);
            await _dbContext.SaveChangesAsync();
            _dbContext.DispatchDetails.Add(new DispatchDetail
            {
                TicketId = trip1.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234",
                VehicleStatus = "Assigned"
            });
            await _dbContext.SaveChangesAsync();

            var driver2 = new Driver { DriverId = 2, DriverName = "Jane Smith", Status = "Active", IsActive = true };
            _dbContext.Drivers.Add(driver2);
            await _dbContext.SaveChangesAsync();

            var trip2 = new TripTicket
            {
                TicketNumber = "REQ-VMX-002",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Approved",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(10, 0),
                EstArrivalTime = new TimeOnly(12, 0),
                PickupLocation = "C", DropoffLocation = "D",
                Purpose = "Trip B"
            };
            _dbContext.TripTickets.Add(trip2);
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = trip2.TicketId, DriverId = 2, VehicleId = 1,
                DriverName = "Jane Smith", PlateNumber = "ABC1234",
                Remarks = "Same vehicle, different driver", Status = "Ready for Dispatch"
            };

            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");
            var vehicleDispatches = await _dbContext.DispatchDetails.CountAsync(d => d.VehicleId == 1);
            vehicleDispatches.Should().Be(2);
        }

        [Fact]
        public async Task AssignDriver_DifferentDriversOnOverlappingTrips_Succeeds()
        {
            SetupUser(10);
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            var trip1 = new TripTicket
            {
                TicketNumber = "REQ-PAR-001",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(12, 0),
                PickupLocation = "A", DropoffLocation = "B",
                Purpose = "Trip A"
            };
            _dbContext.TripTickets.Add(trip1);
            await _dbContext.SaveChangesAsync();
            _dbContext.DispatchDetails.Add(new DispatchDetail
            {
                TicketId = trip1.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234",
                VehicleStatus = "Assigned"
            });
            await _dbContext.SaveChangesAsync();

            var driver2 = new Driver { DriverId = 2, DriverName = "Jane Smith", Status = "Active", IsActive = true };
            var vehicle2 = new Vehicle { VehicleId = 2, PlateNumber = "XYZ5678", VehicleModel = "Bus", Capacity = 20, Status = "Available", IsActive = true };
            _dbContext.Drivers.Add(driver2);
            _dbContext.Vehicles.Add(vehicle2);
            await _dbContext.SaveChangesAsync();

            var trip2 = new TripTicket
            {
                TicketNumber = "REQ-PAR-002",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Approved",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(9, 0),
                EstArrivalTime = new TimeOnly(11, 0),
                PickupLocation = "C", DropoffLocation = "D",
                Purpose = "Parallel trip"
            };
            _dbContext.TripTickets.Add(trip2);
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = trip2.TicketId, DriverId = 2, VehicleId = 2,
                DriverName = "Jane Smith", PlateNumber = "XYZ5678",
                Remarks = "Parallel", Status = "Ready for Dispatch"
            };

            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");
        }

        [Fact]
        public async Task AssignDriver_SameDriverOnDifferentDays_Succeeds()
        {
            SetupUser(10);
            var day1 = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
            var day2 = DateOnly.FromDateTime(DateTime.Today.AddDays(2));

            var trip1 = new TripTicket
            {
                TicketNumber = "REQ-DAY-001",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = day1,
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A", DropoffLocation = "B",
                Purpose = "Day 1"
            };
            _dbContext.TripTickets.Add(trip1);
            await _dbContext.SaveChangesAsync();
            _dbContext.DispatchDetails.Add(new DispatchDetail
            {
                TicketId = trip1.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234",
                VehicleStatus = "Assigned"
            });
            await _dbContext.SaveChangesAsync();

            var trip2 = new TripTicket
            {
                TicketNumber = "REQ-DAY-002",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Approved",
                DateOfTrip = day2,
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "C", DropoffLocation = "D",
                Purpose = "Day 2"
            };
            _dbContext.TripTickets.Add(trip2);
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = trip2.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234",
                Remarks = "Different day", Status = "Ready for Dispatch"
            };

            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");
        }

        [Fact]
        public async Task AssignDriver_ThreeNonOverlappingThenFourthOverlapping_Fails()
        {
            SetupUser(10);
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            var trip1 = new TripTicket
            {
                TicketNumber = "REQ-STG-001",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(6, 0),
                EstArrivalTime = new TimeOnly(8, 0),
                PickupLocation = "A", DropoffLocation = "B", Purpose = "T1"
            };
            _dbContext.TripTickets.Add(trip1);
            await _dbContext.SaveChangesAsync();
            _dbContext.DispatchDetails.Add(new DispatchDetail
            {
                TicketId = trip1.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234", VehicleStatus = "Assigned"
            });
            await _dbContext.SaveChangesAsync();

            var trip2 = new TripTicket
            {
                TicketNumber = "REQ-STG-002",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "C", DropoffLocation = "D", Purpose = "T2"
            };
            _dbContext.TripTickets.Add(trip2);
            await _dbContext.SaveChangesAsync();
            _dbContext.DispatchDetails.Add(new DispatchDetail
            {
                TicketId = trip2.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234", VehicleStatus = "Assigned"
            });
            await _dbContext.SaveChangesAsync();

            var trip3 = new TripTicket
            {
                TicketNumber = "REQ-STG-003",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(10, 0),
                EstArrivalTime = new TimeOnly(12, 0),
                PickupLocation = "E", DropoffLocation = "F", Purpose = "T3"
            };
            _dbContext.TripTickets.Add(trip3);
            await _dbContext.SaveChangesAsync();
            _dbContext.DispatchDetails.Add(new DispatchDetail
            {
                TicketId = trip3.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234", VehicleStatus = "Assigned"
            });
            await _dbContext.SaveChangesAsync();

            var trip4 = new TripTicket
            {
                TicketNumber = "REQ-STG-004",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Approved",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(11, 0),
                EstArrivalTime = new TimeOnly(13, 0),
                PickupLocation = "G", DropoffLocation = "H", Purpose = "T4"
            };
            _dbContext.TripTickets.Add(trip4);
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = trip4.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234",
                Remarks = "Should fail", Status = "Ready for Dispatch"
            };

            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            isSuccess.Should().BeFalse();
            message.Should().Contain("already assigned");
        }

        [Fact]
        public async Task AssignDriver_SameDriverWithGapBetweenTrips_Succeeds()
        {
            SetupUser(10);
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            var trip1 = new TripTicket
            {
                TicketNumber = "REQ-GAP-001",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Ready for Dispatch",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(8, 0),
                EstArrivalTime = new TimeOnly(10, 0),
                PickupLocation = "A", DropoffLocation = "B",
                Purpose = "Morning"
            };
            _dbContext.TripTickets.Add(trip1);
            await _dbContext.SaveChangesAsync();
            _dbContext.DispatchDetails.Add(new DispatchDetail
            {
                TicketId = trip1.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234",
                VehicleStatus = "Assigned"
            });
            await _dbContext.SaveChangesAsync();

            var trip2 = new TripTicket
            {
                TicketNumber = "REQ-GAP-002",
                RequestorId = "req1", RequestorName = "Req",
                ApprovalStatus = "Approved",
                DateOfTrip = date,
                EstDepartureTime = new TimeOnly(11, 0),
                EstArrivalTime = new TimeOnly(13, 0),
                PickupLocation = "C", DropoffLocation = "D",
                Purpose = "Afternoon"
            };
            _dbContext.TripTickets.Add(trip2);
            await _dbContext.SaveChangesAsync();

            var dispatchDto = new DispatchDto
            {
                TicketId = trip2.TicketId, DriverId = 1, VehicleId = 1,
                DriverName = "John Doe", PlateNumber = "ABC1234",
                Remarks = "1-hour gap", Status = "Ready for Dispatch"
            };

            var (isSuccess, message) = await _service.AssignDriver(dispatchDto);

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
