using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using ShuttlOps.DTOs;
using ShuttlOps.Services.Interfaces;

namespace ShuttlOps.Controllers
{
    [Authorize(Roles = "GA,Admin")]
    public class GAController(IGAService _service) : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/GA/Index.cshtml");
        }


        [HttpGet]
        public async Task<IActionResult> GetVehicle()
        {
            var result = await _service.GetAllVehicle();
            return new JsonResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetDrivers()
        {
            var result = await _service.GetAllDriver();
            return new JsonResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetApprovedRequest()
        {
            var result = await _service.GetApproveRequest();
            return new JsonResult(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNewDriver([FromBody] DriverDTO Driver)
        {
            var (success, message) = await _service.CreateDriverDetails(Driver);
            return new JsonResult(new { success, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNewVehicle([FromBody] VehicleDTO Vehicle)
        {
            var (success, message) = await _service.CreateVehicleDetails(Vehicle);
            return new JsonResult(new { success, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDriver([FromBody] DriverDTO Driver)
        {
            var (success, message) = await _service.UpdateDriverDetails(Driver);
            return new JsonResult(new { success, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVehicle([FromBody] VehicleDTO VehicleDetail)
        {
            var (success, message) = await _service.UpdateVehicleDetails(VehicleDetail);
            return new JsonResult(new { success, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDriverRecord(int id)
        {
            var (success, message) = await _service.DeleteDriverDetails(id);
            return new JsonResult(new { success, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVehicleRecord(int id)
        {
            var (success, message) = await _service.DeleteVehicleDetails(id);
            return new JsonResult(new { success, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDriver([FromBody] DispatchDto Dispatch)
        {
            var (success, message) = await _service.AssignDriver(Dispatch);
            return new JsonResult(new { success, message });
        }

      
    } 


}
