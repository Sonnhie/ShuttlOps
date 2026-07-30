using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ShuttlOps.Controllers
{
    public class LoginController : Controller
    {
        private ILogger<LoginController> _logger;
        public LoginController(ILogger<LoginController> logger)
        {
            _logger = logger;
        }

        [HttpGet("/Login")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }
    }
}
