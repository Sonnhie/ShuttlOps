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
using Microsoft.Extensions.Configuration;
using ShuttlOps.Services.Interfaces;

namespace ShuttlOps.Tests.Services
{
    public class AdminServiceTests : IDisposable
    {
        private readonly ShuttlOpsDbContext _dbContext;
        private readonly Mock<ILogger<AdminService>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly AdminService _service;

        public AdminServiceTests()
        {
            var options = new DbContextOptionsBuilder<ShuttlOpsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ShuttlOpsDbContext(options);
            _mockLogger = new Mock<ILogger<AdminService>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockEmailService = new Mock<IEmailService>();
            _mockConfiguration = new Mock<IConfiguration>();

            _service = new AdminService(
                _dbContext,
                _mockLogger.Object,
                _mockHttpContextAccessor.Object,
                _mockEmailService.Object,
                _mockConfiguration.Object);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var dept = new Department { DepartmentId = 1, DepartmentName = "IT" };
            var roleAdmin = new RoleTable { RoleId = 1, RoleName = "Admin" };
            var roleGA = new RoleTable { RoleId = 2, RoleName = "GA" };
            var roleReq = new RoleTable { RoleId = 3, RoleName = "Requestor" };

            _dbContext.Departments.Add(dept);
            _dbContext.RoleTables.AddRange(roleAdmin, roleGA, roleReq);

            _dbContext.UserTables.AddRange(
                new UserTable { Id = 1, UserName = "admin1", EmployeeName = "Admin User", Password = "hashedPassword", EmailAdd = "admin@a.com", RoleId = 1, DepartmentId = 1 },
                new UserTable { Id = 2, UserName = "ga1", EmployeeName = "GA User", Password = "hashedPassword", EmailAdd = "ga@a.com", RoleId = 2, DepartmentId = 1 },
                new UserTable { Id = 3, UserName = "req1", EmployeeName = "Req User", Password = "hashedPassword", EmailAdd = "req@a.com", RoleId = 3, DepartmentId = 1 }
            );

            _dbContext.SaveChanges();
        }

        private void SetupUserRole(string role)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "10"),
                new(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var mockHttpContext = new DefaultHttpContext { User = claimsPrincipal };
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext);
        }

        [Fact]
        public async Task GetAllUsers_WhenCallerIsGA_ExcludesAdminUsers()
        {
            SetupUserRole("GA");

            var users = await _service.GetAllUsers();

            users.Should().NotBeNull();
            users.Should().NotContain(u => u.Role == "Admin");
            users.Should().Contain(u => u.Role == "GA");
            users.Should().Contain(u => u.Role == "Requestor");
        }

        [Fact]
        public async Task GetAllUsers_WhenCallerIsAdmin_IncludesAdminUsers()
        {
            SetupUserRole("Admin");

            var users = await _service.GetAllUsers();

            users.Should().NotBeNull();
            users.Should().Contain(u => u.Role == "Admin");
            users.Should().Contain(u => u.Role == "GA");
            users.Should().Contain(u => u.Role == "Requestor");
        }

        [Fact]
        public async Task CreateUser_WhenCallerIsGAAndRoleIsAdmin_ReturnsError()
        {
            SetupUserRole("GA");

            var userDto = new UserDto
            {
                EmployeeID = "newadmin",
                EmployeeName = "New Admin",
                Email = "newadmin@a.com",
                Department = "1",
                Role = "1" // Admin role ID
            };

            var (isSuccess, message) = await _service.CreateUser(userDto);

            isSuccess.Should().BeFalse();
            message.Should().Contain("cannot create or assign Admin");
        }

        [Fact]
        public async Task CreateUser_WhenCallerIsGAAndRoleIsRequestor_Succeeds()
        {
            SetupUserRole("GA");

            var userDto = new UserDto
            {
                EmployeeID = "newreq",
                EmployeeName = "New Requestor",
                Email = "newreq@a.com",
                Department = "1",
                Role = "3" // Requestor role ID
            };

            var (isSuccess, message) = await _service.CreateUser(userDto);

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");

            var created = await _dbContext.UserTables.FirstOrDefaultAsync(u => u.UserName == "newreq");
            created.Should().NotBeNull();
            created!.RoleId.Should().Be(3);
        }

        [Fact]
        public async Task DeleteUser_WhenCallerIsGAAndTargetIsAdmin_ReturnsError()
        {
            SetupUserRole("GA");

            var (isSuccess, message) = await _service.DeleteUser(1); // Target is Admin (Id=1)

            isSuccess.Should().BeFalse();
            message.Should().Contain("cannot delete Admin");

            var adminUser = await _dbContext.UserTables.FindAsync(1);
            adminUser.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteUser_WhenCallerIsGAAndTargetIsNonAdmin_Succeeds()
        {
            SetupUserRole("GA");

            var (isSuccess, message) = await _service.DeleteUser(3); // Target is Requestor (Id=3)

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");

            var reqUser = await _dbContext.UserTables.FindAsync(3);
            reqUser.Should().BeNull();
        }

        [Fact]
        public async Task ResetPassword_WhenCallerIsGAAndTargetIsAdmin_ReturnsError()
        {
            SetupUserRole("GA");

            var (isSuccess, message) = await _service.ResetPassword(1); // Target is Admin (Id=1)

            isSuccess.Should().BeFalse();
            message.Should().Contain("cannot reset Admin");
        }

        [Fact]
        public async Task ResetPassword_WhenCallerIsGAAndTargetIsNonAdmin_Succeeds()
        {
            SetupUserRole("GA");

            var (isSuccess, message) = await _service.ResetPassword(3); // Target is Requestor (Id=3)

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
