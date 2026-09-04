using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShuttlOps.DTOs;
using ShuttlOps.Hubs;
using ShuttlOps.Models.Temp;
using ShuttlOps.Services.MainServices;
using ShuttlOps.Models;
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
        private readonly ShuttlOpsDbContext _dbContext;
        private readonly NotificationService _service;
        private readonly string _dbName;

        public NotificationServiceTests()
        {
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();
            _mockLogger = new Mock<ILogger<NotificationService>>();

            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
            _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockClients.Setup(c => c.Groups(It.IsAny<IReadOnlyList<string>>())).Returns(_mockClientProxy.Object);
            _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);

            _dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<ShuttlOpsDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options;
            _dbContext = new ShuttlOpsDbContext(options);

            _service = new NotificationService(_mockHubContext.Object, _dbContext, _mockLogger.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task SendToRoleAsync_WhenValid_SendsToRoleGroups()
        {
            var notif = new NotificationMessageDTO { Title = "New Trip Created", Message = "Trip ticket REQ-001 created", Category = "Ticket" };
            await _service.SendToRoleAsync("GA", notif);
            _mockClients.Verify(c => c.Groups(It.Is<IReadOnlyList<string>>(g => g.Contains("Role_GA") && g.Count == 1)), Times.Once);
            _mockClientProxy.Verify(p => p.SendCoreAsync("ReceiveNotification", It.IsAny<object[]>(), default(CancellationToken)), Times.Once);
        }

        [Fact]
        public async Task SendToDepartmentAsync_WhenValid_SendsToDeptGroups()
        {
            var notif = new NotificationMessageDTO { Title = "Section Approval Needed", Message = "New ticket in IT department", Category = "Approval" };
            await _service.SendToDepartmentAsync("IT", notif);
            _mockClients.Verify(c => c.Groups(It.Is<IReadOnlyList<string>>(g => g.Contains("Dept_IT") && g.Count == 1)), Times.Once);
            _mockClientProxy.Verify(p => p.SendCoreAsync("ReceiveNotification", It.IsAny<object[]>(), default(CancellationToken)), Times.Once);
        }

        [Fact]
        public async Task BroadcastAsync_WhenValid_SendsToAll()
        {
            var notif = new NotificationMessageDTO { Title = "System Notice", Message = "Maintenance in 1 hour", Category = "General" };
            await _service.BroadcastAsync(notif);
            _mockClients.Verify(c => c.All, Times.Once);
            _mockClientProxy.Verify(p => p.SendCoreAsync("ReceiveNotification", It.IsAny<object[]>(), default(CancellationToken)), Times.Once);
        }

        [Fact]
        public async Task GetUserNotificationsAsync_ReturnsEmpty_WhenNoNotifications()
        {
            var result = await _service.GetUserNotificationsAsync(1);
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUnreadCountAsync_ReturnsZero_WhenNoNotifications()
        {
            var count = await _service.GetUnreadCountAsync(1);
            count.Should().Be(0);
        }
    }
}