using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShuttlOps.DTOs;
using ShuttlOps.Services.Interfaces;

namespace ShuttlOps.Controllers
{
    [Authorize(Roles = "Admin,GA")]
    public class AdminController(IAdminservice adminservice) : Controller
    {
        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            return View("~/Views/Pages/Admin/Index.cshtml");
        }

        public IActionResult Users()
        {
            return View("~/Views/Pages/Admin/Users.cshtml");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Groups()
        {
            return View("~/Views/Pages/Admin/Groups.cshtml");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Modules()
        {
            return View("~/Views/Pages/Admin/Modules.cshtml");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Access()
        {
            return View("~/Views/Pages/Admin/Access.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetUser()
        {
            var users = await adminservice.GetAllUsers();
            return new JsonResult(new
            {
                success = true,
                data = users
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await adminservice.GetDepartments();
            return new JsonResult(new
            {
                success = true,
                data = departments
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await adminservice.GetRoles();
            return new JsonResult(new
            {
                success = true,
                data = roles
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserDto userDto)
        {
            var (isSuccess, message) = await adminservice.CreateUser(userDto);
            return new JsonResult(new
            {
                success = isSuccess,
                message = message
            });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var (isSuccess, message) = await adminservice.DeleteUser(id);
            return new JsonResult(new
            {
                success = isSuccess,
                message = message
            });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var (isSuccess, message) = await adminservice.ResetPassword(id);
            return new JsonResult(new
            {
                success = isSuccess,
                message = message
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllDepartments()
        {
            var departments = await adminservice.GetAllDepartments();
            return new JsonResult(new
            {
                success = true,
                data = departments
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var (isSuccess, message) = await adminservice.DeleteDepartment(id);
            return new JsonResult(new
            {
                success = isSuccess,
                message = message
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateDepartment([FromBody] DepartmentDTO departmentDto)
        {
            var (isSuccess, message) = await adminservice.CreateDepartment(departmentDto);
            return new JsonResult(new
            {
                success = isSuccess,
                message = message
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetModules()
        {
            var modules = await adminservice.GetModules();
            return new JsonResult(new
            {
                success = true,
                data = modules
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetModulesSelection()
        {
            var modules = await adminservice.GetModule();
            return new JsonResult(new
            {
                success = true,
                data = modules
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateModuleStatus([FromBody] ModuleDTO moduleDTO)
        {
            var (isSuccess, message) = await adminservice.UpdateModuleStatus(moduleDTO);
            return new JsonResult(new
            {
                success = isSuccess,
                message = message
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateModule([FromBody] ModuleDTO moduleDTO)
        {
            var (isSuccess, message) = await adminservice.CreateModule(moduleDTO);
            return new JsonResult(new
            {
                success = isSuccess,
                message = message
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetPermissionList()
        {
            var permissions = await adminservice.GetPermissionList();
            return new JsonResult(new
            {
                success = true,
                data = permissions
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePermissionAction(AccessPayloadDTO payload)
        {
            var (isSuccess, message) = await adminservice.UpdatePermission(payload);
            return new JsonResult(new
            {
                success = isSuccess,
                message = message
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreatePermission([FromBody] AccessDTO payload)
        {
            var (isSuccess, message) = await adminservice.CreatePermission(payload);
            return new JsonResult(new
            {
                success = isSuccess,
                message = message
            });
        }
    }
}
