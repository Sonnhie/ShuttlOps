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

                return new JsonResult(new
                {
                    success = false,
                    message = firstError
                });
            }

            var (isSuccess, message) = await requestService.CreateRequest(request);
            if (isSuccess)
            {
                return new JsonResult(new
                {
                    success = true,
                    message = message,
                    redirectUrl = "/User/TripSchedule"
                });
            }
            else
            {
                return new JsonResult(new
                {
                    success = false,
                    message = message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRequests()
        {
            var requests = await requestService.GetAllRequests();
            return new JsonResult(new
            {
                success = true,
                data = requests
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetScheduledTrip()
        {
            var requests = await requestService.GetScheduledTrip();
            return new JsonResult(new
            {
                success = true,
                data = requests
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetScheduledOnTrip()
        {
            var requests = await requestService.GetOnTripScheduled();
            return new JsonResult(new
            {
                success = true,
                data = requests
            });
        }

        [HttpPost]
        public async Task<IActionResult> ProcessApproval(string status, int id)
        {
            var (isSuccess, message) = await requestService.ProcessApproval(status, id);
            if (isSuccess)
            {
                return new JsonResult(new
                {
                    success = true,
                    message = message
                });
            }
            else
            {
                return new JsonResult(new
                {
                    success = false,
                    message = message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRequestApproval(int id)
        {
            var (isSuccess, message) = await requestService.DeleteRequestApproval(id);
            if (isSuccess)
            {
                return new JsonResult(new
                {
                    success = true,
                    message
                });
            }
            else
            {
                return new JsonResult(new
                {
                    success = false,
                    message
                });
            }
        }

        [Authorize(Roles ="Security")]
        [HttpPost]
        public async Task<IActionResult> SecurityLogs([FromBody] SecurityLogsDTO securityLogsDTO)
        {
            var (isSuccess, message) = await requestService.SecurityLogs(securityLogsDTO);
            if (isSuccess)
            {
                return new JsonResult(new
                {
                    success = true,
                    message
                });
            }
            else
            {
                return new JsonResult(new
                {
                    success = false,
                    message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDrivers()
        {
            var result = await requestService.GetDriver();
            return new JsonResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetVehicle()
        {
            var result = await requestService.GetVehicle();
            return new JsonResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetPlatenumber(int id)
        {
            var result = await requestService.GetPlatenumber(id);
            return new JsonResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetCapacity(int id)
        {
            var result = await requestService.GetCapacity(id);
            return new JsonResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetVehicleStatus(int id)
        {
            var result = await requestService.GetVehicleStatus(id);
            return new JsonResult(result);
        }

        [Authorize(Roles ="Security")]
        [HttpPost]
        public async Task<IActionResult> DispatchConfirm(int ticketId)
        {
            var (isSuccess, message) = await requestService.DispatchConfirm(ticketId);
            if (isSuccess)
            {
                return new JsonResult(new
                {
                    success = true,
                    message
                });
            }
            else
            {
                return new JsonResult(new
                {
                    success = false,
                    message
                });
            }
        }
    }
}

