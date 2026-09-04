using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShuttlOps.DTOs;
using ShuttlOps.Models;
using ShuttlOps.Services.MainServices;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ShuttlOps.Tests.Services
{
    public class GAServicesCrudTests : IDisposable
    {
        private readonly ShuttlOpsDbContext _dbContext;
        private readonly Mock<ILogger<GAServices>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly GAServices _service;

        public GAServicesCrudTests()
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
            _dbContext.Vehicles.Add(new Vehicle { VehicleId = 1, PlateNumber = "ABC1234", VehicleModel = "Van", Capacity = 10, Status = "Available", IsActive = true });
            _dbContext.Vehicles.Add(new Vehicle { VehicleId = 2, PlateNumber = "XYZ5678", VehicleModel = "Bus", Capacity = 30, Status = "Available", IsActive = true });
            _dbContext.Drivers.Add(new Driver { DriverId = 1, DriverName = "John Doe", LicenseNumber = "LIC001", ContactNumber = "09123456789", Status = "Active", IsActive = true });
            _dbContext.Drivers.Add(new Driver { DriverId = 2, DriverName = "Jane Smith", LicenseNumber = "LIC002", ContactNumber = "09987654321", Status = "Available", IsActive = true });
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task CreateDriverDetails_WhenValid_AddsDriverAndReturnsSuccess()
        {
            var driverDto = new DriverDTO
            {
                DriverName = "Carlos Mendoza",
                LicenseNumber = "N01-99-887766",
                ContactNumber = "09171234567",
                Status = "Available"
            };

            var (isSuccess, message) = await _service.CreateDriverDetails(driverDto);

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");

            var created = await _dbContext.Drivers.FirstOrDefaultAsync(d => d.LicenseNumber == "N01-99-887766");
            created.Should().NotBeNull();
            created!.DriverName.Should().Be("Carlos Mendoza");
            created.Status.Should().Be("Available");
            created.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task CreateDriverDetails_WhenNull_ReturnsError()
        {
            var (isSuccess, message) = await _service.CreateDriverDetails(null!);

            isSuccess.Should().BeFalse();
            message.Should().Contain("empty");
        }

        [Fact]
        public async Task UpdateDriverDetails_WhenValid_UpdatesDriverAndReturnsSuccess()
        {
            var updateDto = new DriverDTO
            {
                Id = 1,
                DriverName = "John Doe Updated",
                LicenseNumber = "N01-11-222222",
                ContactNumber = "09998887777",
                Status = "On Leave"
            };

            var (isSuccess, message) = await _service.UpdateDriverDetails(updateDto);

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");

            var updated = await _dbContext.Drivers.FindAsync(1);
            updated!.DriverName.Should().Be("John Doe Updated");
            updated.LicenseNumber.Should().Be("N01-11-222222");
            updated.ContactNumber.Should().Be("09998887777");
            updated.Status.Should().Be("On Leave");
        }

        [Fact]
        public async Task UpdateDriverDetails_WhenDriverNotFound_ReturnsError()
        {
            var updateDto = new DriverDTO
            {
                Id = 9999,
                DriverName = "Nonexistent",
                LicenseNumber = "N00-00-000000",
                ContactNumber = "0000",
                Status = "Available"
            };

            var (isSuccess, message) = await _service.UpdateDriverDetails(updateDto);

            isSuccess.Should().BeFalse();
            message.Should().Contain("not registered");
        }

        [Fact]
        public async Task DeleteDriverDetails_WhenValid_RemovesDriverAndReturnsSuccess()
        {
            var (isSuccess, message) = await _service.DeleteDriverDetails(2);

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");

            var deleted = await _dbContext.Drivers.FindAsync(2);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteDriverDetails_WhenNotFound_ReturnsError()
        {
            var (isSuccess, message) = await _service.DeleteDriverDetails(9999);

            isSuccess.Should().BeFalse();
            message.Should().Contain("not exist");
        }

        [Fact]
        public async Task CreateVehicleDetails_WhenValid_AddsVehicleAndReturnsSuccess()
        {
            var vehicleDto = new VehicleDTO
            {
                VehicleModel = "Toyota Coaster",
                PlateNumber = "XYZ-9999",
                Capacity = 25,
                Status = "Available"
            };

            var (isSuccess, message) = await _service.CreateVehicleDetails(vehicleDto);

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");

            var created = await _dbContext.Vehicles.FirstOrDefaultAsync(v => v.PlateNumber == "XYZ-9999");
            created.Should().NotBeNull();
            created!.VehicleModel.Should().Be("Toyota Coaster");
            created.Capacity.Should().Be(25);
            created.Status.Should().Be("Available");
            created.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task CreateVehicleDetails_WhenNull_ReturnsError()
        {
            var (isSuccess, message) = await _service.CreateVehicleDetails(null!);

            isSuccess.Should().BeFalse();
            message.Should().Contain("empty");
        }

        [Fact]
        public async Task UpdateVehicleDetails_WhenValid_UpdatesVehicleAndReturnsSuccess()
        {
            var updateDto = new VehicleDTO
            {
                Id = 1,
                VehicleModel = "Toyota Hiace Grandia",
                PlateNumber = "ABC-9999",
                Capacity = 16,
                Status = "Under Maintenance"
            };

            var (isSuccess, message) = await _service.UpdateVehicleDetails(updateDto);

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");

            var updated = await _dbContext.Vehicles.FindAsync(1);
            updated!.VehicleModel.Should().Be("Toyota Hiace Grandia");
            updated.PlateNumber.Should().Be("ABC-9999");
            updated.Capacity.Should().Be(16);
            updated.Status.Should().Be("Under Maintenance");
        }

        [Fact]
        public async Task UpdateVehicleDetails_WhenVehicleNotFound_ReturnsError()
        {
            var updateDto = new VehicleDTO
            {
                Id = 9999,
                VehicleModel = "Nonexistent",
                PlateNumber = "NONE-00",
                Capacity = 4,
                Status = "Available"
            };

            var (isSuccess, message) = await _service.UpdateVehicleDetails(updateDto);

            isSuccess.Should().BeFalse();
            message.Should().Contain("does not exist");
        }

        [Fact]
        public async Task DeleteVehicleDetails_WhenValid_RemovesVehicleAndReturnsSuccess()
        {
            var (isSuccess, message) = await _service.DeleteVehicleDetails(2);

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");

            var deleted = await _dbContext.Vehicles.FindAsync(2);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteVehicleDetails_WhenNotFound_ReturnsError()
        {
            var (isSuccess, message) = await _service.DeleteVehicleDetails(9999);

            isSuccess.Should().BeFalse();
            message.Should().Contain("does not exist");
        }

        [Fact]
        public async Task GetAllDriver_ReturnsMappedDriverList()
        {
            var drivers = await _service.GetAllDriver();

            drivers.Should().NotBeNull();
            drivers.Count.Should().BeGreaterOrEqualTo(2);
            drivers.Should().Contain(d => d.DriverName == "John Doe");
        }

        [Fact]
        public async Task GetAllVehicle_ReturnsMappedVehicleList()
        {
            var vehicles = await _service.GetAllVehicle();

            vehicles.Should().NotBeNull();
            vehicles.Count.Should().BeGreaterOrEqualTo(2);
            vehicles.Should().Contain(v => v.PlateNumber == "ABC1234");
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
