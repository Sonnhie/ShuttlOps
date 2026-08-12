using ShuttlOps.DTOs;

namespace ShuttlOps.Services
{
    public interface IAdminservice
    {
        Task<List<UserDto>> GetAllUsers();
        Task<object> GetDepartments();
        Task<object> GetRoles();
        Task<(bool isSuccess, string message)> CreateUser(UserDto userDto);
        Task<(bool isSuccess, string message)> DeleteUser(int id);
        Task<(bool isSuccess, string message)> ResetPassword(int id);
        Task<List<DepartmentDTO>> GetAllDepartments();
        Task<(bool isSuccess, string message)> DeleteDepartment(int id);
        Task<(bool isSuccess, string message)> CreateDepartment(DepartmentDTO departmentDTO);
        Task<List<ModuleDTO>> GetModules();
        Task<object> GetModule();
        Task<(bool isSuccess, string message)> UpdateModuleStatus(ModuleDTO moduleDTO);
        Task<(bool isSuccess, string message)> CreateModule(ModuleDTO moduleDTO);
        Task<List<AccessDTO>> GetPermissionList();
        Task<(bool isSuccess, string message)> UpdatePermission(AccessPayloadDTO payload);
        Task<(bool isSuccess, string message)> CreatePermission(AccessDTO access);
    }
}