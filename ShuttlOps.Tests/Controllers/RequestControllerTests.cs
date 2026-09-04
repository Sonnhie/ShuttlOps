using Microsoft.AspNetCore.Mvc;
using Moq;
using ShuttlOps.Controllers;
using ShuttlOps.DTOs;
using ShuttlOps.Services.Interfaces;
using System.Text.Json;
using Xunit;

namespace ShuttlOps.Tests.Controllers;

public class RequestControllerTests
{
    private readonly Mock<IRequestService> requestService = new();
    private readonly RequestController controller;

    public RequestControllerTests() => controller = new RequestController(requestService.Object);

    [Fact]
    public async Task CreateRequest_WhenModelIsValidAndServiceSucceeds_ReturnsSuccessPayloadWithRedirect()
    {
        var request = new CreateTripTicketDTO
        {
            TripDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            DepartureTime = new TimeOnly(8, 0),
            ArrivalTime = new TimeOnly(9, 0),
            PickupLocation = "Main HQ",
            DropLocation = "Plant 2",
            Purpose = "Inspection"
        };
        requestService.Setup(s => s.CreateRequest(request)).ReturnsAsync((true, "Trip ticket REQ-20260901-0001 created successfully."));

        var result = await controller.CreateRequest(request);

        var json = Assert.IsType<JsonResult>(result);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(json.Value));
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Trip ticket REQ-20260901-0001 created successfully.", document.RootElement.GetProperty("message").GetString());
        Assert.Equal("/User/TripSchedule", document.RootElement.GetProperty("redirectUrl").GetString());
        requestService.Verify(s => s.CreateRequest(request), Times.Once);
    }

    [Fact]
    public async Task CreateRequest_WhenModelHasErrors_ReturnsFailureWithoutCallingService()
    {
        controller.ModelState.AddModelError("PickupLocation", "Pickup location is required.");
        var request = new CreateTripTicketDTO();

        var result = await controller.CreateRequest(request);

        AssertJson(result, false, "Pickup location is required.");
        requestService.Verify(s => s.CreateRequest(It.IsAny<CreateTripTicketDTO>()), Times.Never);
    }

    [Fact]
    public async Task CreateRequest_WhenServiceFails_ReturnsFailurePayload()
    {
        var request = new CreateTripTicketDTO
        {
            TripDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            DepartureTime = new TimeOnly(8, 0),
            ArrivalTime = new TimeOnly(9, 0),
            PickupLocation = "Main HQ",
            DropLocation = "Plant 2",
            Purpose = "Inspection"
        };
        requestService.Setup(s => s.CreateRequest(request)).ReturnsAsync((false, "Estimated arrival time must be later than estimated departure time."));

        var result = await controller.CreateRequest(request);

        AssertJson(result, false, "Estimated arrival time must be later than estimated departure time.");
    }

    [Fact]
    public async Task GetAllRequests_ReturnsServiceData()
    {
        var requests = new List<TripTicketResponseDTO>
        {
            new() { TicketId = 12, TicketNumber = "REQ-20260824-0012", Requestor = "Alex", RequestDepartment = "IT" }
        };
        requestService.Setup(s => s.GetAllRequests()).ReturnsAsync(requests);

        var result = await controller.GetAllRequests();

        var json = Assert.IsType<JsonResult>(result);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(json.Value));
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("REQ-20260824-0012", document.RootElement.GetProperty("data")[0].GetProperty("TicketNumber").GetString());
    }

    [Fact]
    public async Task DeleteRequestApproval_WhenServiceFails_ReturnsFailurePayload()
    {
        requestService.Setup(s => s.DeleteRequestApproval(9)).ReturnsAsync((false, "Trip ticket does not exist."));

        var result = await controller.DeleteRequestApproval(9);

        AssertJson(result, false, "Trip ticket does not exist.");
        requestService.Verify(s => s.DeleteRequestApproval(9), Times.Once);
    }

    private static void AssertJson(IActionResult result, bool success, string message)
    {
        var json = Assert.IsType<JsonResult>(result);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(json.Value));
        Assert.Equal(success, document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(message, document.RootElement.GetProperty("message").GetString());
    }
}

