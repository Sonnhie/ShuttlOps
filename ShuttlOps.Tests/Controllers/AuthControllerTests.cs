using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ShuttlOps.Controllers;
using ShuttlOps.Services.Interfaces;
using ShuttlOps.ViewModel.Auth;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace ShuttlOps.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthenticationservice> authenticationService = new();
    private readonly AuthController controller;

    public AuthControllerTests()
    {
        controller = new AuthController(authenticationService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task LoggedIn_WhenModelIsInvalid_ReturnsValidationErrorWithoutAuthenticating()
    {
        controller.ModelState.AddModelError("UsernameInput", "Username is required.");

        var result = await controller.LoggedIn(new LoginViewModel());

        AssertJson(result, false, "Username is required.");
        authenticationService.Verify(s => s.AuthenticateUser(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoggedIn_WhenAdminAuthenticates_ReturnsAdminRedirect()
    {
        authenticationService.Setup(s => s.AuthenticateUser("admin1", "Password01"))
            .ReturnsAsync((true, "Authentication Successfull.", "Admin"));

        var result = await controller.LoggedIn(new LoginViewModel { UsernameInput = "admin1", PasswordInput = "Password01" });

        var json = Assert.IsType<JsonResult>(result);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(json.Value));
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("/Admin/Index", document.RootElement.GetProperty("redirectUrl").GetString());
    }

    [Fact]
    public async Task LoggedIn_WhenGAAuthenticates_ReturnsGARedirect()
    {
        authenticationService.Setup(s => s.AuthenticateUser("ga1", "Password01"))
            .ReturnsAsync((true, "Authentication Successfull.", "GA"));

        var result = await controller.LoggedIn(new LoginViewModel { UsernameInput = "ga1", PasswordInput = "Password01" });

        var json = Assert.IsType<JsonResult>(result);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(json.Value));
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("/GA/Index", document.RootElement.GetProperty("redirectUrl").GetString());
    }

    [Fact]
    public async Task LoggedIn_WhenSecurityAuthenticates_ReturnsSecurityLogsRedirect()
    {
        authenticationService.Setup(s => s.AuthenticateUser("sec1", "Password01"))
            .ReturnsAsync((true, "Authentication Successfull.", "Security"));

        var result = await controller.LoggedIn(new LoginViewModel { UsernameInput = "sec1", PasswordInput = "Password01" });

        var json = Assert.IsType<JsonResult>(result);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(json.Value));
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("/User/SecurityLogs", document.RootElement.GetProperty("redirectUrl").GetString());
    }

    [Theory]
    [InlineData("Requestor", "/User/Index")]
    [InlineData("Section Approver", "/User/Index")]
    public async Task LoggedIn_WhenRequestorOrApproverAuthenticates_ReturnsUserIndexRedirect(string role, string expectedUrl)
    {
        authenticationService.Setup(s => s.AuthenticateUser("user1", "Password01"))
            .ReturnsAsync((true, "Authentication Successfull.", role));

        var result = await controller.LoggedIn(new LoginViewModel { UsernameInput = "user1", PasswordInput = "Password01" });

        var json = Assert.IsType<JsonResult>(result);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(json.Value));
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(expectedUrl, document.RootElement.GetProperty("redirectUrl").GetString());
    }

    [Fact]
    public async Task Logout_ReturnsServiceResult()
    {
        authenticationService.Setup(s => s.Logout()).ReturnsAsync((true, "Logged out."));

        var result = await controller.Logout();

        AssertJson(result, true, "Logged out.");
    }

    private static void AssertJson(IActionResult result, bool success, string message)
    {
        var json = Assert.IsType<JsonResult>(result);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(json.Value));
        Assert.Equal(success, document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(message, document.RootElement.GetProperty("message").GetString());
    }
}
