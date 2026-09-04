using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ShuttlOps.DTOs;
using ShuttlOps.Services.Interfaces;
using ShuttlOps.ViewModel.Auth;
using System.Security.Claims;

namespace ShuttlOps.Controllers
{
    public class AuthController(IAuthenticationservice authenticationService) : Controller
    {
        [HttpPost("/Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoggedIn(LoginViewModel Input)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault();
                return new JsonResult(new
                {
                    success = false,
                    message = firstError?.ToString()
                });
            }

            var (isSuccess, message, role) = await authenticationService.AuthenticateUser(Input.UsernameInput, Input.PasswordInput);

            if (!isSuccess)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = message
                });
            }

            var effectiveRole = role ?? HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            var redirectUrl = GetDashboardUrlForRole(effectiveRole);

            return new JsonResult(new
            {
                success = true,
                message = message,
                redirectUrl = redirectUrl
            });
        }

        public async Task<IActionResult> Logout()
        {
            var (isSuccess, message) = await authenticationService.Logout();
            return new JsonResult(new
            {
                success = isSuccess,
                message = message,
                RedirectToAction = isSuccess ? "/Account/Login" : null
            });
        }

        [Authorize]
        [HttpPost("/Account/ChangePassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault();
                return new JsonResult(new
                {
                    success = false,
                    message = firstError?.ToString() ?? "Validation error."
                });
            }

            var (isSuccess, message) = await authenticationService.ChangePassword(dto);
            return new JsonResult(new
            {
                success = isSuccess,
                message = message
            });
        }

        public static string GetDashboardUrlForRole(string? role)
        {
            return role switch
            {
                "Admin" => "/Admin/Index",
                "GA" => "/GA/Index",
                "Security" => "/User/SecurityLogs",
                "Requestor" or "Section Approver" => "/User/Index",
                _ => "/User/Index"
            };
        }
    }
}
