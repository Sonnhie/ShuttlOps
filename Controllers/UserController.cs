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

        public IActionResult Analytics()
        {
            return View("~/Views/Pages/User/Analytics.cshtml");
        }

        public IActionResult Reports()
        {
            return View("~/Views/Pages/User/Reports.cshtml");
        }

        public IActionResult SecurityLogs()
        {
            return View("~/Views/Pages/User/SecurityLogs.cshtml");
        }

        public IActionResult ScheduleCalendar()
        {
            return View("~/Views/Pages/User/ScheduleCalendar.cshtml");
        }

        public IActionResult Settings()
        {
            return View("~/Views/Pages/User/Settings.cshtml");
        }

        public IActionResult ReservationManagement()
        {
            return View("~/Views/Pages/User/ReservationManagement.cshtml");
        }

        public IActionResult ChangePassword()
        {
            return View("~/Views/Pages/User/ChangePassword.cshtml");
        }
    }
}
