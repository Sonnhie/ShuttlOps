using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShuttlOps.DTOs;
using ShuttlOps.Services;
using System.Net.WebSockets;

namespace ShuttlOps.Controllers
{
    [Authorize]
    public class RequestController(IRequestService requestService) : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

       
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateTripTicketDTO request)
        {
            var (isSuccess, message) = await requestService.CreateRequest(request);
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

        [HttpPost]
        public async Task<IActionResult> SectionRequestApproval(string status, int id)
        {
            var (isSuccess, message) = await requestService.RequestApproval(status, id);
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
        public async Task<IActionResult> GARequestApproval(string status, int id)
        {
            var (isSuccess, message) = await requestService.RequestApproval(status, id);
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
        public async Task<IActionResult> RequestDelete(string status, int id)
        {
            var (isSuccess, message) = await requestService.RequestApproval(status, id);
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
    }
}
