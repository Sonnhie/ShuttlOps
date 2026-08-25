using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ShuttlOps.Controllers;
using ShuttlOps.Services;
using ShuttlOps.ViewModel.Auth;
using System.Security.Claims;
using System.Text.Json;

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
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")], "Test"));
        authenticationService.Setup(s => s.AuthenticateUser("admin1", "Password01")).ReturnsAsync((true, "Authentication Successfull."));

        var result = await controller.LoggedIn(new LoginViewModel { UsernameInput = "admin1", PasswordInput = "Password01" });

        var json = Assert.IsType<JsonResult>(result);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(json.Value));
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("/Admin/Index", document.RootElement.GetProperty("redirectUrl").GetString());
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
