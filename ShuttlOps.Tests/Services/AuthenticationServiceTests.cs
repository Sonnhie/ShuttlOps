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
    public class AuthenticationServiceTests : IDisposable
    {
        private readonly ShuttlOpsDbContext _dbContext;
        private readonly Mock<ILogger<Authenticationservice>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Authenticationservice _service;

        public AuthenticationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ShuttlOpsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ShuttlOpsDbContext(options);
            _mockLogger = new Mock<ILogger<Authenticationservice>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            _service = new Authenticationservice(_dbContext, _mockLogger.Object, _mockHttpContextAccessor.Object);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var dept = new Department { DepartmentId = 1, DepartmentName = "IT" };
            var role = new RoleTable { RoleId = 1, RoleName = "Requestor" };

            _dbContext.Departments.Add(dept);
            _dbContext.RoleTables.Add(role);

            string hashed = BCrypt.Net.BCrypt.HashPassword("OldPassword123");

            _dbContext.UserTables.Add(
                new UserTable { Id = 1, UserName = "user1", EmployeeName = "Test User", Password = hashed, EmailAdd = "u@a.com", RoleId = 1, DepartmentId = 1 }
            );

            _dbContext.SaveChanges();
        }

        private void SetupAuthenticatedUser(int userId)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, "Test User"),
                new(ClaimTypes.Role, "Requestor")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var mockHttpContext = new DefaultHttpContext { User = claimsPrincipal };
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext);
        }

        [Fact]
        public async Task ChangePassword_WhenValid_UpdatesPasswordAndReturnsSuccess()
        {
            SetupAuthenticatedUser(1);

            var dto = new ChangePasswordDTO
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "NewSecretPassword456!",
                ConfirmPassword = "NewSecretPassword456!"
            };

            var (isSuccess, message) = await _service.ChangePassword(dto);

            isSuccess.Should().BeTrue();
            message.Should().Contain("successfully");

            var user = await _dbContext.UserTables.FindAsync(1);
            BCrypt.Net.BCrypt.Verify("NewSecretPassword456!", user!.Password).Should().BeTrue();
        }

        [Fact]
        public async Task ChangePassword_WhenCurrentPasswordIncorrect_ReturnsError()
        {
            SetupAuthenticatedUser(1);

            var dto = new ChangePasswordDTO
            {
                CurrentPassword = "WrongPassword999",
                NewPassword = "NewSecretPassword456!",
                ConfirmPassword = "NewSecretPassword456!"
            };

            var (isSuccess, message) = await _service.ChangePassword(dto);

            isSuccess.Should().BeFalse();
            message.Should().Contain("Current password is incorrect");
        }

        [Fact]
        public async Task ChangePassword_WhenNewPasswordMismatch_ReturnsError()
        {
            SetupAuthenticatedUser(1);

            var dto = new ChangePasswordDTO
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "NewSecretPassword456!",
                ConfirmPassword = "DifferentPassword789!"
            };

            var (isSuccess, message) = await _service.ChangePassword(dto);

            isSuccess.Should().BeFalse();
            message.Should().Contain("do not match");
        }

        [Fact]
        public async Task ChangePassword_WhenNewPasswordTooShort_ReturnsError()
        {
            SetupAuthenticatedUser(1);

            var dto = new ChangePasswordDTO
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "123",
                ConfirmPassword = "123"
            };

            var (isSuccess, message) = await _service.ChangePassword(dto);

            isSuccess.Should().BeFalse();
            message.Should().Contain("at least 6 characters");
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
