using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ShuttlOps.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;

        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
        }

        [HttpGet("/Account/Login")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                var redirectUrl = AuthController.GetDashboardUrlForRole(role);
                return Redirect(redirectUrl);
            }

            return View();
        }

        [HttpGet("/Account/ForgotPassword")]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }
    }
}
