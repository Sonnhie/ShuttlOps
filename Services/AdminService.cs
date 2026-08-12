using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShuttlOps.DTOs;
using ShuttlOps.Models;
using System.Reflection.Metadata.Ecma335;

namespace ShuttlOps.Services
{
    [Authorize(Roles = "Admin")]
    public class AdminService(
        ShuttlOpsDbContext dbContext,
        ILogger<AdminService> logger
    ) : IAdminservice
    {
        public async Task<List<UserDto>> GetAllUsers()
        {
            try
            {
                var result = await dbContext.UserTables
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        EmployeeID = u.UserName,
                        EmployeeName = u.EmployeeName,
                        Email = u.EmailAdd,
                        Department = u.Department.DepartmentName ?? string.Empty,
                        Role = u.Role.RoleName,
                    })
                    .ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all users.");
                return [];
            }
        }

        public async Task<object> GetDepartments()
        {
            try
            {
                var result = await dbContext.Departments
                            .Select(x => new { x.DepartmentId, x.DepartmentName })
                            .ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all departments.");
                return Array.Empty<object>();
            }
        }

        public async Task<object> GetRoles()
        {
            try
            {
                var result = await dbContext.RoleTables
                            .Select(r => new { r.RoleId, r.RoleName })
                            .ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all departments.");
                return Array.Empty<object>();
            }
        }

        public async Task<object> GetModule()
        {
            try
            {
                var result = await dbContext.ModulesTables
                            .Select(r => new { r.ModuleId, r.ModuleName, r.IsActive })
                            .ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all modules.");
                return Array.Empty<object>();
            }
        }

        public async Task<(bool isSuccess, string message)> CreateUser(UserDto userDto)
        {
            try
            {
                if(userDto == null)
                {
                    return (false, "User data is null.");
                }

                var isUserExist = await dbContext.UserTables.FirstOrDefaultAsync(x => x.UserName == userDto.EmployeeID);

                if (isUserExist != null)
                {
                    return (false, "User already exit on the database.");
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("Password01");

                int? departmentId = int.TryParse(userDto.Department, out int parseId) ? parseId : null;
                int? roleId = int.TryParse(userDto.Role, out int parseRoldId) ? parseRoldId : null;
                var newUser = new UserTable
                {
                    UserName = userDto.EmployeeID,
                    Password = hashedPassword,
                    EmailAdd = userDto.Email,
                    DepartmentId = parseId,
                    RoleId = parseRoldId,
                    EmployeeName = userDto.EmployeeName,
                    CreatedAt = DateTime.Now,
                };

                dbContext.UserTables.Add(newUser);
                await dbContext.SaveChangesAsync();
                return (true, "User created successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating a new user.");
                return (false, $"Error creating user: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> DeleteUser(int id)
        {
            try
            {
                var isUserExist = await dbContext.UserTables.FirstOrDefaultAsync(x => x.Id == id);
                if (isUserExist == null)
                {
                    return (false, "User not exist on the database.");
                }

                dbContext.UserTables.Remove(isUserExist);
                await dbContext.SaveChangesAsync();
                return (true, "User account successfully deleted.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while deleting a user.");
                return (false, $"Error deleting user: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> ResetPassword(int id)
        {
            try
            {
                var isUserExist = await dbContext.UserTables.FirstOrDefaultAsync(x => x.Id==id);
                if (isUserExist == null)
                {
                    return (false, "User not exist on the database.");
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("Password01");
                isUserExist.Password = hashedPassword;
                await dbContext.SaveChangesAsync();
                return (true, "Password reset successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while resetting password.");
                return (false, $"Error resetting password: {ex.Message}");
            }
        }

        public async Task<List<DepartmentDTO>> GetAllDepartments()
        {
            try
            {
                var result = await dbContext.Departments
                             .Select(d => new DepartmentDTO
                             {
                                 Id = d.DepartmentId,
                                 DepartmentName = d.DepartmentName ?? string.Empty,
                                 ManagerId = d.ManagerId,
                                 MembersCount = d.UserTables.Count()
                             })
                             .ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all departments.");
                return [];
            }
        }

        public async Task<(bool isSuccess, string message)> DeleteDepartment(int id)
        {
            try
            {
                var isDepartmentExist = await dbContext.Departments.FirstOrDefaultAsync(x => x.DepartmentId == id);
                if (isDepartmentExist == null)
                {
                    return (false, "Department not exist on the database.");
                }

                dbContext.Departments.Remove(isDepartmentExist);
                await dbContext.SaveChangesAsync();
                return (true, "Department successfully deleted.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while deleting a department.");
                return (false, $"Error deleting department: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> CreateDepartment(DepartmentDTO departmentDTO)
        {
            try
            {
                var isDepartmentExist = await dbContext.Departments.FirstOrDefaultAsync(x => x.DepartmentName == departmentDTO.DepartmentName);
                if (isDepartmentExist != null)
                {
                    return (false, "Department already exist on the database.");
                }

                var newDepartment = new Department
                {
                    DepartmentName = departmentDTO.DepartmentName,
                    ManagerId = departmentDTO.ManagerId ?? 0
                };
                
                dbContext.Departments.Add(newDepartment);
                await dbContext.SaveChangesAsync();
                return (true, "Department successfully created.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while deleting a department.");
                return (false, $"Error deleting department: {ex.Message}");
            }
        }

        public async Task<List<ModuleDTO>> GetModules()
        {
            try
            {
                var modules = await dbContext.ModulesTables
            .Select(m => new ModuleDTO
            {
                Id = m.ModuleId,
                ModuleName = m.ModuleName,
                ModuleDescription = m.Description ?? string.Empty,
                IsActive = m.IsActive
            }).ToListAsync();
                return modules;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all departments.");
                return [];
            }
        }

        public async Task<(bool isSuccess, string message)> UpdateModuleStatus(ModuleDTO moduleDTO)
        {
            try
            {
                var module = await dbContext.ModulesTables.FindAsync(moduleDTO.Id);
                if (module == null)
                {
                    return (false, "Module does not exist in database.");
                }

                module.IsActive = moduleDTO.IsActive;
                await dbContext.SaveChangesAsync();
                string statusWord = moduleDTO.IsActive ? "activated" : "deactivated";
                return (true, $"Module successfully {statusWord}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while deleting a department.");
                return (false, $"Error deleting department: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> CreateModule(ModuleDTO moduleDTO)
        {
            try
            {
                var module = await dbContext.ModulesTables.FirstOrDefaultAsync(x => x.ModuleName == moduleDTO.ModuleName);
                if (module != null)
                {
                    return (false, "Module already exist on the system.");
                }

                var NewModule = new ModulesTable
                {
                    ModuleName = moduleDTO.ModuleName,
                    Description = moduleDTO.ModuleDescription,
                    IsActive = false,
                };
                dbContext.ModulesTables.Add(NewModule);
                await dbContext.SaveChangesAsync();
                return (true, "Module successfully created.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating a module.");
                return (false, $"Error creating module: {ex.Message}");
            }
        }

        public async Task<List<AccessDTO>> GetPermissionList()
        {
            try
            {
                var result = await dbContext.PermissionsTables
                             .Include(p => p.Role)
                             .Include(p => p.Module)
                             .Select(p => new AccessDTO
                             {
                                 Permission_id = p.PermissionId,
                                 Role_id = p.RoleId,
                                 Module_id = p.ModuleId,
                                 Role_name = p.Role.RoleName,
                                 Module_name = p.Module.ModuleName,
                                 Can_view = p.CanView,
                                 Can_create = p.CanCreate,
                                 Can_approve = p.CanApprove,
                                 Can_delete = p.CanDelete,
                                 Can_edit = p.CanEdit
                             })
                             .ToListAsync();

                return result;
            }catch(Exception ex)
            {
                logger.LogError(ex, "An error occurred while getting the data.");
                return [];
            }
        }

        public async Task<(bool isSuccess, string message)> UpdatePermission(AccessPayloadDTO payload)
        {
            if (string.IsNullOrWhiteSpace(payload?.action))
            {
                return (false, "Action parameter is required.");
            }

            try
            {
                var permission = await dbContext.PermissionsTables.FindAsync(payload.id);
                if (permission == null)
                {
                    return (false, $"Module with ID {payload.id} was not found.");
                }

                string cleanAction = payload.action.Trim();

                if (cleanAction == "Can_view")
                {
                    permission.CanView = payload.isActive;
                }
                else if(cleanAction == "Can_create")
                {
                    permission.CanCreate = payload.isActive;
                }
                else if(cleanAction == "Can_delete")
                {
                    permission.CanDelete = payload.isActive;
                }
                else if(cleanAction == "Can_approve")
                {
                    permission.CanApprove = payload.isActive;
                }
                else if(cleanAction == "Can_edit")
                {
                    permission.CanEdit = payload.isActive;
                }

                await dbContext.SaveChangesAsync();
                string statusWord = payload.isActive ? "enable action" : "disable action";
                return (true, $"Successfully {statusWord}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating a permission.");
                return (false, $"Error creating module: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> CreatePermission(AccessDTO access)
        {
            try
            {
                var permissionExist = await dbContext.PermissionsTables.Where(p => p.RoleId == access.Role_id && p.ModuleId == access.Module_id).FirstOrDefaultAsync();
                if(permissionExist != null)
                {
                    return (false, $"This permission is already exist.");
                }

                var newPermission = new PermissionsTable();


                if (access.SelectAll)
                {
                    newPermission.RoleId = access.Role_id;
                    newPermission.ModuleId = access.Module_id;
                    newPermission.CanApprove = true;
                    newPermission.CanCreate = true;
                    newPermission.CanDelete = true;
                    newPermission.CanEdit = true;
                    newPermission.CanView = true;
                }
                else
                {
                    newPermission.RoleId = access.Role_id;
                    newPermission.ModuleId = access.Module_id;
                    newPermission.CanApprove = access.Can_approve;
                    newPermission.CanCreate = access.Can_create;
                    newPermission.CanDelete = access.Can_delete;
                    newPermission.CanEdit = access.Can_edit;
                    newPermission.CanView = access.Can_view;
                }

                dbContext.PermissionsTables.Add(newPermission);
                await dbContext.SaveChangesAsync();
                return (true, "Permission successfully created.");
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating a permission.");
                return (false, $"Error creating module: {ex.Message}");
            }
        }
    }
}
