using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ShuttlOps.Services;
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

            var (isSuccess, message) = await authenticationService.AuthenticateUser(Input.UsernameInput, Input.PasswordInput);
            
            string redirectUrl;

            var roleClaim = HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;

            if (roleClaim == "Admin")
            {
                redirectUrl = "/Admin/Index";
            }
            else
            {
                redirectUrl = "/User/Index";
            }

            if (!isSuccess)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = message
                });
            }

            return new JsonResult(new
            {
                success = isSuccess,
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
    }
}
