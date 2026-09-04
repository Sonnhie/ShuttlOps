using Microsoft.AspNetCore.Mvc;
using ShuttlOps.Controllers;
using Xunit;

namespace ShuttlOps.Tests.Controllers;

public class UserControllerTests
{
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _controller = new UserController();
    }

    [Fact]
    public void Index_WhenCalled_ReturnsIndexView()
    {
        // Act
        var result = _controller.Index() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("~/Views/Pages/User/Index.cshtml", result!.ViewName);
    }

    [Fact]
    public void CreateTicketPage_WhenCalled_ReturnsCreateTicketPageView()
    {
        // Act
        var result = _controller.CreateTicketPage() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("~/Views/Pages/User/Create_ticket_page.cshtml", result!.ViewName);
    }

    [Fact]
    public void TripSchedule_WhenCalled_ReturnsTripScheduleView()
    {
        // Act
        var result = _controller.TripSchedule() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("~/Views/Pages/User/TripSchedule.cshtml", result!.ViewName);
    }

    [Fact]
    public void Analytics_WhenCalled_ReturnsAnalyticsView()
    {
        // Act
        var result = _controller.Analytics() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("~/Views/Pages/User/Analytics.cshtml", result!.ViewName);
    }

    [Fact]
    public void Reports_WhenCalled_ReturnsReportsView()
    {
        // Act
        var result = _controller.Reports() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("~/Views/Pages/User/Reports.cshtml", result!.ViewName);
    }

    [Fact]
    public void SecurityLogs_WhenCalled_ReturnsSecurityLogsView()
    {
        // Act
        var result = _controller.SecurityLogs() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("~/Views/Pages/User/SecurityLogs.cshtml", result!.ViewName);
    }

    [Fact]
    public void ScheduleCalendar_WhenCalled_ReturnsScheduleCalendarView()
    {
        // Act
        var result = _controller.ScheduleCalendar() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("~/Views/Pages/User/ScheduleCalendar.cshtml", result!.ViewName);
    }

    [Fact]
    public void Settings_WhenCalled_ReturnsSettingsView()
    {
        // Act
        var result = _controller.Settings() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("~/Views/Pages/User/Settings.cshtml", result!.ViewName);
    }

    [Fact]
    public void ReservationManagement_WhenCalled_ReturnsReservationManagementView()
    {
        // Act
        var result = _controller.ReservationManagement() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("~/Views/Pages/User/ReservationManagement.cshtml", result!.ViewName);
    }
}

