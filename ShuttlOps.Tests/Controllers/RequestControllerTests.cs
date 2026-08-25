using Microsoft.AspNetCore.Mvc;
using Moq;
using ShuttlOps.Controllers;
using ShuttlOps.DTOs;
using ShuttlOps.Services;
using System.Text.Json;

namespace ShuttlOps.Tests.Controllers;

public class RequestControllerTests
{
    private readonly Mock<IRequestService> requestService = new();
    private readonly RequestController controller;

    public RequestControllerTests() => controller = new RequestController(requestService.Object);

    [Fact]
    public async Task CreateRequest_WhenServiceSucceeds_ReturnsSuccessPayload()
    {
        var request = new CreateTripTicketDTO { PickupLocation = "Office", DropLocation = "Airport", Purpose = "Site visit" };
        requestService.Setup(s => s.CreateRequest(request)).ReturnsAsync((true, "Request created successfully."));

        var result = await controller.CreateRequest(request);

        AssertJson(result, true, "Request created successfully.");
        requestService.Verify(s => s.CreateRequest(request), Times.Once);
    }

    [Fact]
    public async Task CreateRequest_WhenServiceFails_ReturnsFailurePayload()
    {
        var request = new CreateTripTicketDTO();
        requestService.Setup(s => s.CreateRequest(request)).ReturnsAsync((false, "Invalid request payload."));

        var result = await controller.CreateRequest(request);

        AssertJson(result, false, "Invalid request payload.");
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
