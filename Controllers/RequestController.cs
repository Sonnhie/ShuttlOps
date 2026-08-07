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
    }
}
