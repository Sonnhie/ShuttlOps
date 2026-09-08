using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShuttlOps.DTOs;
using ShuttlOps.Hubs;
using ShuttlOps.Models;
using ShuttlOps.Models.Temp;
using ShuttlOps.Services.MainServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ShuttlOps.Tests.Services
{
    public class NotificationServiceTests : IDisposable
    {
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly Mock<IHubClients> _mockClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<ILogger<NotificationService>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly ShuttlOpsDbContext _dbContext;
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();
            _mockLogger = new Mock<ILogger<NotificationService>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
            _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockClients.Setup(c => c.Groups(It.IsAny<IReadOnlyList<string>>())).Returns(_mockClientProxy.Object);
            _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);

            var options = new DbContextOptionsBuilder<ShuttlOpsDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ShuttlOpsDbContext(options);

            _service = new NotificationService(
                _mockHubContext.Object,
                _dbContext,
                _mockHttpContextAccessor.Object,
                _mockLogger.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────

        private RoleTable SeedRole(int id, string name)
        {
            var role = new RoleTable { RoleId = id, RoleName = name };
            _dbContext.RoleTables.Add(role);
            return role;
        }

        private Department SeedDepartment(int id, string name, int? managerId = null)
        {
            var dept = new Department { DepartmentId = id, DepartmentName = name, ManagerId = managerId };
            _dbContext.Departments.Add(dept);
            return dept;
        }

        private UserTable SeedUser(int id, string userName, int roleId, int departmentId)
        {
            var user = new UserTable
            {
                Id = id,
                UserName = userName,
                EmployeeName = userName,
                Password = "pw",
                EmailAdd = $"{userName}@test.com",
                RoleId = roleId,
                DepartmentId = departmentId
            };
            _dbContext.UserTables.Add(user);
            return user;
        }

        private static NotificationMessageDTO BuildNotification(string title = "Test", string msg = "Test message", string category = "General")
            => new() { Title = title, Message = msg, Category = category };

        // ── SendToRoleAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task SendToRoleAsync_WhenValid_SendsToRoleGroup()
        {
            var notif = BuildNotification("New Trip Created", "Trip ticket REQ-001 created", "Ticket");

            await _service.SendToRoleAsync("GA", notif);

            _mockClients.Verify(c => c.Groups(It.Is<IReadOnlyList<string>>(g =>
                g.Contains("Role_GA") && g.Count == 1)), Times.Once);
            _mockClientProxy.Verify(p => p.SendCoreAsync(
                "ReceiveNotification", It.IsAny<object[]>(), default(CancellationToken)), Times.Once);
        }

        [Fact]
        public async Task SendToRoleAsync_WhenUsersExist_SavesNotificationForEachUser()
        {
            var role = SeedRole(1, "GA");
            var dept = SeedDepartment(1, "IT");
            SeedUser(10, "ga_user1", role.RoleId, dept.DepartmentId);
            SeedUser(11, "ga_user2", role.RoleId, dept.DepartmentId);
            await _dbContext.SaveChangesAsync();

            await _service.SendToRoleAsync("GA", BuildNotification());

            var saved = _dbContext.Notifications.ToList();
            saved.Should().HaveCount(2);
            saved.Select(n => n.UserId).Should().BeEquivalentTo(new[] { 10, 11 });
        }

        [Fact]
        public async Task SendToRoleAsync_WhenRoleIsNull_DoesNotSend()
        {
            await _service.SendToRoleAsync(null!, BuildNotification());

            _mockClientProxy.Verify(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Never);
        }

        [Fact]
        public async Task SendToRoleAsync_WhenNotificationIsNull_DoesNotSend()
        {
            await _service.SendToRoleAsync("GA", null!);

            _mockClientProxy.Verify(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Never);
        }

        // ── SendToRolesAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task SendToRolesAsync_WhenValidRoles_SendsToAllRoleGroups()
        {
            var roles = new[] { "GA", "Security" };

            await _service.SendToRolesAsync(roles, BuildNotification());

            _mockClients.Verify(c => c.Groups(It.Is<IReadOnlyList<string>>(g =>
                g.Contains("Role_GA") && g.Contains("Role_Security"))), Times.Once);
        }

        [Fact]
        public async Task SendToRolesAsync_WhenRolesIsNull_DoesNotSend()
        {
            await _service.SendToRolesAsync(null!, BuildNotification());

            _mockClientProxy.Verify(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Never);
        }

        // ── SendToDepartmentAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task SendToDepartmentAsync_WhenValid_SendsToDeptGroup()
        {
            var notif = BuildNotification("Section Approval Needed", "New ticket in IT", "Approval");

            await _service.SendToDepartmentAsync("IT", notif);

            _mockClients.Verify(c => c.Groups(It.Is<IReadOnlyList<string>>(g =>
                g.Contains("Dept_IT") && g.Count == 1)), Times.Once);
            _mockClientProxy.Verify(p => p.SendCoreAsync(
                "ReceiveNotification", It.IsAny<object[]>(), default(CancellationToken)), Times.Once);
        }

        [Fact]
        public async Task SendToDepartmentAsync_WhenDepartmentIsEmpty_DoesNotSend()
        {
            await _service.SendToDepartmentAsync("", BuildNotification());

            _mockClientProxy.Verify(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Never);
        }

        // ── SendToRoleInDepartmentAsync ──────────────────────────────────────────────

        [Fact]
        public async Task SendToRoleInDepartmentAsync_WhenUsersExist_SavesNotificationAndSendsToRoleGroup()
        {
            var role = SeedRole(2, "Section Approver");
            var dept = SeedDepartment(1, "Finance");
            SeedUser(20, "sa_user1", role.RoleId, dept.DepartmentId);
            await _dbContext.SaveChangesAsync();

            await _service.SendToRoleInDepartmentAsync("Section Approver", "Finance", BuildNotification("SA Notif", "msg"));

            var saved = _dbContext.Notifications.ToList();
            saved.Should().HaveCount(1);
            saved[0].UserId.Should().Be(20);

            _mockClients.Verify(c => c.Groups(It.Is<IReadOnlyList<string>>(g =>
                g.Contains("Role_Section Approver"))), Times.Once);
        }

        [Fact]
        public async Task SendToRoleInDepartmentAsync_WhenNoMatchingUsers_SavesNoNotifications()
        {
            await _service.SendToRoleInDepartmentAsync("Section Approver", "NonExistentDept", BuildNotification());

            _dbContext.Notifications.Should().BeEmpty();
        }

        [Fact]
        public async Task SendToRoleInDepartmentAsync_WhenRoleIsEmpty_DoesNotSend()
        {
            await _service.SendToRoleInDepartmentAsync("", "Finance", BuildNotification());

            _mockClientProxy.Verify(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Never);
        }

        // ── SendToSectionHeadOfDepartmentAsync ───────────────────────────────────────

        [Fact]
        public async Task SendToSectionHeadOfDepartmentAsync_WhenDepartmentHasManager_SavesNotificationForManager()
        {
            var role = SeedRole(3, "Section Approver");
            var dept = SeedDepartment(5, "HR", managerId: 50);
            SeedUser(50, "sectionhead1", role.RoleId, dept.DepartmentId);
            await _dbContext.SaveChangesAsync();

            await _service.SendToSectionHeadOfDepartmentAsync("HR", BuildNotification("New Request", "Ticket submitted for HR", "Approval"));

            var saved = _dbContext.Notifications.ToList();
            saved.Should().HaveCount(1);
            saved[0].UserId.Should().Be(50);
            saved[0].Title.Should().Be("New Request");
        }

        [Fact]
        public async Task SendToSectionHeadOfDepartmentAsync_WhenDepartmentHasManager_SendsToUserGroup()
        {
            var role = SeedRole(3, "Section Approver");
            var dept = SeedDepartment(5, "HR", managerId: 50);
            SeedUser(50, "sectionhead1", role.RoleId, dept.DepartmentId);
            await _dbContext.SaveChangesAsync();

            await _service.SendToSectionHeadOfDepartmentAsync("HR", BuildNotification());

            _mockClients.Verify(c => c.Groups(It.Is<IReadOnlyList<string>>(g =>
                g.Contains("User_50"))), Times.Once);
            _mockClientProxy.Verify(p => p.SendCoreAsync(
                "ReceiveNotification", It.IsAny<object[]>(), default), Times.Once);
        }

        [Fact]
        public async Task SendToSectionHeadOfDepartmentAsync_WhenSectionHeadManagesMultipleDepts_NotifiedForEachDept()
        {
            // One Section Head (ID=60) manages two separate departments via ManagerId
            var role = SeedRole(3, "Section Approver");
            SeedDepartment(10, "Accounting", managerId: 60);
            SeedDepartment(11, "Payroll", managerId: 60);
            SeedUser(60, "sectionhead_multi", role.RoleId, 10);
            await _dbContext.SaveChangesAsync();

            await _service.SendToSectionHeadOfDepartmentAsync("Accounting", BuildNotification("Acct Notif", "msg1"));
            await _service.SendToSectionHeadOfDepartmentAsync("Payroll", BuildNotification("Payroll Notif", "msg2"));

            var saved = _dbContext.Notifications.OrderBy(n => n.NotificationId).ToList();
            saved.Should().HaveCount(2);
            saved.Should().AllSatisfy(n => n.UserId.Should().Be(60));
            saved[0].Title.Should().Be("Acct Notif");
            saved[1].Title.Should().Be("Payroll Notif");
        }

        [Fact]
        public async Task SendToSectionHeadOfDepartmentAsync_WhenMultipleManagersForDept_NotifiesBothManagers()
        {
            // Two departments share the same name but have different managers
            var role = SeedRole(3, "Section Approver");
            SeedDepartment(20, "Operations", managerId: 70);
            SeedDepartment(21, "Operations", managerId: 71);
            SeedUser(70, "sh_ops1", role.RoleId, 20);
            SeedUser(71, "sh_ops2", role.RoleId, 21);
            await _dbContext.SaveChangesAsync();

            await _service.SendToSectionHeadOfDepartmentAsync("Operations", BuildNotification("Ops Notif", "msg"));

            var saved = _dbContext.Notifications.ToList();
            saved.Should().HaveCount(2);
            saved.Select(n => n.UserId).Should().BeEquivalentTo(new[] { 70, 71 });

            _mockClients.Verify(c => c.Groups(It.Is<IReadOnlyList<string>>(g =>
                g.Contains("User_70") && g.Contains("User_71"))), Times.Once);
        }

        [Fact]
        public async Task SendToSectionHeadOfDepartmentAsync_WhenDepartmentNotFound_DoesNotSendOrSave()
        {
            await _service.SendToSectionHeadOfDepartmentAsync("NonExistentDept", BuildNotification());

            _dbContext.Notifications.Should().BeEmpty();
            _mockClientProxy.Verify(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Never);
        }

        [Fact]
        public async Task SendToSectionHeadOfDepartmentAsync_WhenDepartmentHasNoManagerId_DoesNotSendOrSave()
        {
            SeedDepartment(30, "Unmanaged", managerId: null);
            await _dbContext.SaveChangesAsync();

            await _service.SendToSectionHeadOfDepartmentAsync("Unmanaged", BuildNotification());

            _dbContext.Notifications.Should().BeEmpty();
            _mockClientProxy.Verify(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Never);
        }

        [Fact]
        public async Task SendToSectionHeadOfDepartmentAsync_WhenDepartmentNameIsEmpty_DoesNotSendOrSave()
        {
            await _service.SendToSectionHeadOfDepartmentAsync("", BuildNotification());

            _dbContext.Notifications.Should().BeEmpty();
            _mockClientProxy.Verify(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Never);
        }

        [Fact]
        public async Task SendToSectionHeadOfDepartmentAsync_WhenNotificationIsNull_DoesNotThrow()
        {
            SeedDepartment(5, "HR", managerId: 50);
            await _dbContext.SaveChangesAsync();

            var act = async () => await _service.SendToSectionHeadOfDepartmentAsync("HR", null!);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SendToSectionHeadOfDepartmentAsync_NotifiesByManagerId_NotByHomeDepartment()
        {
            // Section Head (ID=80) belongs to dept "Admin" but manages "Engineering" via ManagerId
            var sectionHeadRole = SeedRole(3, "Section Approver");
            SeedDepartment(40, "Admin", managerId: null);
            SeedDepartment(41, "Engineering", managerId: 80);
            SeedUser(80, "sh_eng", sectionHeadRole.RoleId, 40); // home dept is Admin, manages Engineering
            await _dbContext.SaveChangesAsync();

            await _service.SendToSectionHeadOfDepartmentAsync("Engineering", BuildNotification("Eng Notif", "msg"));

            // User 80 must receive it even though their home department is Admin
            var saved = _dbContext.Notifications.ToList();
            saved.Should().HaveCount(1);
            saved[0].UserId.Should().Be(80,
                "section head is resolved by Department.ManagerId, not by the user's own DepartmentId");
        }

        // ── SendToUserAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task SendToUserAsync_WhenUserExists_SavesNotificationAndSendsToUserGroup()
        {
            var role = SeedRole(1, "Requestor");
            SeedDepartment(1, "IT");
            SeedUser(5, "req_user", role.RoleId, 1);
            await _dbContext.SaveChangesAsync();

            await _service.SendToUserAsync("req_user", BuildNotification("Trip Approved", "Your trip is approved"));

            var saved = _dbContext.Notifications.ToList();
            saved.Should().HaveCount(1);
            saved[0].UserId.Should().Be(5);

            _mockClients.Verify(c => c.Groups(It.Is<IReadOnlyList<string>>(g =>
                g.Contains("User_5"))), Times.Once);
        }

        [Fact]
        public async Task SendToUserAsync_WhenUserNotFound_DoesNotSaveNotification()
        {
            await _service.SendToUserAsync("nonexistent_user", BuildNotification());

            _dbContext.Notifications.Should().BeEmpty();
            _mockClientProxy.Verify(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Never);
        }

        [Fact]
        public async Task SendToUserAsync_WhenUserIdIsNull_DoesNotSend()
        {
            await _service.SendToUserAsync(null!, BuildNotification());

            _mockClientProxy.Verify(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Never);
        }

        // ── BroadcastAsync ───────────────────────────────────────────────────────────

        [Fact]
        public async Task BroadcastAsync_WhenValid_SendsToAll()
        {
            await _service.BroadcastAsync(BuildNotification("System Notice", "Maintenance in 1 hour"));

            _mockClients.Verify(c => c.All, Times.Once);
            _mockClientProxy.Verify(p => p.SendCoreAsync(
                "ReceiveNotification", It.IsAny<object[]>(), default(CancellationToken)), Times.Once);
        }

        [Fact]
        public async Task BroadcastAsync_WhenNotificationIsNull_DoesNotSend()
        {
            await _service.BroadcastAsync(null!);

            _mockClientProxy.Verify(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Never);
        }

        // ── GetUserNotificationsAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetUserNotificationsAsync_ReturnsEmpty_WhenNoNotifications()
        {
            var result = await _service.GetUserNotificationsAsync(1);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserNotificationsAsync_ReturnsOnlyNotificationsForSpecifiedUser()
        {
            _dbContext.Notifications.AddRange(
                new Notification { UserId = 1, Title = "A", Message = "m", Category = "General", Url = "#", IsRead = false, CreatedAt = DateTime.Now },
                new Notification { UserId = 1, Title = "B", Message = "m", Category = "General", Url = "#", IsRead = false, CreatedAt = DateTime.Now },
                new Notification { UserId = 2, Title = "C", Message = "m", Category = "General", Url = "#", IsRead = false, CreatedAt = DateTime.Now }
            );
            await _dbContext.SaveChangesAsync();

            var result = await _service.GetUserNotificationsAsync(1);

            result.Should().HaveCount(2);
            result.Should().AllSatisfy(n => n.UserId.Should().Be(1));
        }

        [Fact]
        public async Task GetUserNotificationsAsync_RespectsDefaultTakeLimit()
        {
            for (int i = 1; i <= 25; i++)
                _dbContext.Notifications.Add(new Notification
                {
                    UserId = 1, Title = $"N{i}", Message = "m", Category = "General",
                    Url = "#", IsRead = false, CreatedAt = DateTime.Now.AddMinutes(-i)
                });
            await _dbContext.SaveChangesAsync();

            var result = await _service.GetUserNotificationsAsync(1);

            result.Should().HaveCount(20);
        }

        // ── GetUnreadCountAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetUnreadCountAsync_ReturnsZero_WhenNoNotifications()
        {
            var count = await _service.GetUnreadCountAsync(1);

            count.Should().Be(0);
        }

        [Fact]
        public async Task GetUnreadCountAsync_ReturnsOnlyUnreadCount()
        {
            _dbContext.Notifications.AddRange(
                new Notification { UserId = 1, Title = "A", Message = "m", Category = "General", Url = "#", IsRead = false, CreatedAt = DateTime.Now },
                new Notification { UserId = 1, Title = "B", Message = "m", Category = "General", Url = "#", IsRead = true,  CreatedAt = DateTime.Now },
                new Notification { UserId = 1, Title = "C", Message = "m", Category = "General", Url = "#", IsRead = false, CreatedAt = DateTime.Now }
            );
            await _dbContext.SaveChangesAsync();

            var count = await _service.GetUnreadCountAsync(1);

            count.Should().Be(2);
        }

        // ── MarkAsReadAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task MarkAsReadAsync_WhenExists_SetsIsReadToTrue()
        {
            _dbContext.Notifications.Add(new Notification
            {
                NotificationId = 1, UserId = 1, Title = "A", Message = "m",
                Category = "General", Url = "#", IsRead = false, CreatedAt = DateTime.Now
            });
            await _dbContext.SaveChangesAsync();

            await _service.MarkAsReadAsync(1, 1);

            var updated = await _dbContext.Notifications.FindAsync(1);
            updated!.IsRead.Should().BeTrue();
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenNotificationBelongsToDifferentUser_DoesNotMark()
        {
            _dbContext.Notifications.Add(new Notification
            {
                NotificationId = 2, UserId = 99, Title = "A", Message = "m",
                Category = "General", Url = "#", IsRead = false, CreatedAt = DateTime.Now
            });
            await _dbContext.SaveChangesAsync();

            await _service.MarkAsReadAsync(2, 1); // caller is user 1, not 99

            var notif = await _dbContext.Notifications.FindAsync(2);
            notif!.IsRead.Should().BeFalse();
        }

        // ── MarkAllAsReadAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task MarkAllAsReadAsync_MarksAllUnreadForUser()
        {
            _dbContext.Notifications.AddRange(
                new Notification { UserId = 1, Title = "A", Message = "m", Category = "General", Url = "#", IsRead = false, CreatedAt = DateTime.Now },
                new Notification { UserId = 1, Title = "B", Message = "m", Category = "General", Url = "#", IsRead = false, CreatedAt = DateTime.Now },
                new Notification { UserId = 2, Title = "C", Message = "m", Category = "General", Url = "#", IsRead = false, CreatedAt = DateTime.Now }
            );
            await _dbContext.SaveChangesAsync();

            await _service.MarkAllAsReadAsync(1);

            var user1Notifs = _dbContext.Notifications.Where(n => n.UserId == 1).ToList();
            user1Notifs.Should().AllSatisfy(n => n.IsRead.Should().BeTrue());

            var user2Notif = _dbContext.Notifications.First(n => n.UserId == 2);
            user2Notif.IsRead.Should().BeFalse();
        }
    }
}

