using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShuttlOps.DTOs;
using ShuttlOps.Services.Interfaces;

namespace ShuttlOps.Controllers
{
    [Authorize]
    public class RequestController(IRequestService requestService) : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Pages/User/ReservationManagement.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRequest([FromBody] CreateTripTicketDTO request)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Invalid request payload.";
                return Json(new { success = false, message = firstError });
            }
            var (isSuccess, message) = await requestService.CreateRequest(request);
            if (isSuccess)
                return Json(new { success = true, message, redirectUrl = "/User/TripSchedule" });
            else
                return Json(new { success = false, message });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRequests()
        {
            var requests = await requestService.GetAllRequests();
            return Json(new { success = true, data = requests });
        }

        [HttpGet]
        public async Task<IActionResult> GetScheduledTrip()
        {
            var requests = await requestService.GetScheduledTrip();
            return Json(new { success = true, data = requests });
        }

        [HttpGet]
        public async Task<IActionResult> GetScheduledOnTrip()
        {
            var requests = await requestService.GetOnTripScheduled();
            return Json(new { success = true, data = requests });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessApproval(string status, int id)
        {
            var (isSuccess, message) = await requestService.ProcessApproval(status, id);
            return Json(new { success = isSuccess, message });
        }

        [Authorize(Roles = "Admin,GA,Section Approver,Requestor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRequestApproval(int id)
        {
            var (isSuccess, message) = await requestService.DeleteRequestApproval(id);
            return Json(new { success = isSuccess, message });
        }

        [Authorize(Roles = "Security")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SecurityLogs([FromBody] SecurityLogsDTO securityLogsDTO)
        {
            var (isSuccess, message) = await requestService.SecurityLogs(securityLogsDTO);
            return Json(new { success = isSuccess, message });
        }

        [HttpGet]
        public async Task<IActionResult> GetDrivers()
        {
            var result = await requestService.GetDriver();
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetVehicle()
        {
            var result = await requestService.GetVehicle();
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetPlatenumber(int id)
        {
            var result = await requestService.GetPlatenumber(id);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetCapacity(int id)
        {
            var result = await requestService.GetCapacity(id);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetVehicleStatus(int id)
        {
            var result = await requestService.GetVehicleStatus(id);
            return Json(result);
        }

        [Authorize(Roles = "Security")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DispatchConfirm(int ticketId)
        {
            var (isSuccess, message) = await requestService.DispatchConfirm(ticketId);
            return Json(new { success = isSuccess, message });
        }
    }
}