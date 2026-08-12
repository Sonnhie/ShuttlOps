using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShuttlOps.DTOs;
using ShuttlOps.Services;

namespace ShuttlOps.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController(IAdminservice adminservice) : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Pages/Admin/Index.cshtml");
        }

        public IActionResult Users()
        {
            return View("~/Views/Pages/Admin/Users.cshtml");
        }

        public IActionResult Groups()
        {
            return View("~/Views/Pages/Admin/Groups.cshtml");
        }

        public IActionResult Modules()
        {
            return View("~/Views/Pages/Admin/Modules.cshtml");
        }

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
        public async Task<IActionResult> DeleteUser(int id)
        {
            var (isSuccess, message) = await adminservice.DeleteUser(id);
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
        public async Task<IActionResult> ResetPassword(int id)
        {
            var (isSuccess, message) = await adminservice.ResetPassword(id);
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
        public async Task<IActionResult> GetAllDepartments()
        {
            var departments = await adminservice.GetAllDepartments();
            return new JsonResult(new
            {
                success = true,
                data = departments
            });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var (isSuccess, message) = await adminservice.DeleteDepartment(id);
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
        public async Task<IActionResult> CreateDepartment([FromBody] DepartmentDTO departmentDto)
        {
            var (isSuccess, message) = await adminservice.CreateDepartment(departmentDto);
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
        public async Task<IActionResult> GetModules()
        {
            var modules = await adminservice.GetModules();
            return new JsonResult(new
            {
                success = true,
                data = modules
            });
        }

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

        [HttpPost]
        public async Task<IActionResult> UpdateModuleStatus([FromBody] ModuleDTO moduleDTO)
        {
            var (isSuccess, message) = await adminservice.UpdateModuleStatus(moduleDTO);
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
        public async Task<IActionResult> CreateModule([FromBody] ModuleDTO moduleDTO)
        {
            var (isSuccess, message) = await adminservice.CreateModule(moduleDTO);
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
        public async Task<IActionResult> GetPermissionList()
        {
            var permissions = await adminservice.GetPermissionList();
            return new JsonResult(new
            {
                success = true,
                data = permissions
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePermissionAction(AccessPayloadDTO payload)
        {
            var (isSuccess, message) = await adminservice.UpdatePermission(payload);
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
        public async Task<IActionResult> CreatePermission([FromBody] AccessDTO payload)
        {
            var (isSuccess, message) = await adminservice.CreatePermission(payload);
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
    }
}
