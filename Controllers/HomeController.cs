using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShuttlOps.Models;

namespace ShuttlOps.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [Authorize]
        public IActionResult Index()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return role switch
            {
                "Admin" => RedirectToAction("Index", "Admin"),
                "GA" => RedirectToAction("Index", "GA"),
                "Security" => RedirectToAction("SecurityLogs", "User"),
                "Requestor" or "Section Approver" => RedirectToAction("Index", "User"),
                _ => RedirectToAction("Index", "User")
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
