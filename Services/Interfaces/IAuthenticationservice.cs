using ShuttlOps.DTOs;

namespace ShuttlOps.Services.Interfaces
{
    public interface IAuthenticationservice
    {
        Task<(bool isSuccess, bool changePassRequired, string message, string? role)> AuthenticateUser(string username, string password);
        Task<(bool isSuccess, string message)> Logout();
        Task<(bool isSuccess, string message)> ChangePassword(ChangePasswordDTO dto);
    }
}
