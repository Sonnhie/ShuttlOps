using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ShuttlOps.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Pages/User/Index.cshtml");
        }

        public IActionResult CreateTicketPage()
        {
            return View("~/Views/Pages/User/Create_ticket_page.cshtml");
        }

        public IActionResult TripSchedule()
        {
            return View("~/Views/Pages/User/TripSchedule.cshtml");
        }

        public IActionResult ShuttleViewer()
        {
            return View("~/Views/Pages/User/ShuttleViewer.cshtml");
        }
    }
}
