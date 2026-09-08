using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using ShuttlOps.DTOs;
using ShuttlOps.Models;
using ShuttlOps.Services.Interfaces;
using System.Security.Claims;

namespace ShuttlOps.Services.MainServices
{
    public class Authenticationservice(
        ShuttlOpsDbContext dbcontext,
        ILogger<Authenticationservice> logger,
        IHttpContextAccessor httpContextAccessor) : IAuthenticationservice
    {

        public async Task<(bool isSuccess, bool changePassRequired, string message, string? role)> AuthenticateUser(string username, string password)
        {
            try
            {
                var user = await dbcontext.UserTables
                                .Where(x => x.UserName == username)
                                .Include(m => m.Role)
                                .Include(m => m.Department)
                                .FirstOrDefaultAsync();
                if (user == null)
                {
                    return (false, false, "Invalid username or password.", null);
                }

                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
                if (!isPasswordValid)
                {
                    return (false, false, "Incorrect password.", null);
                }

                var roleName = user.Role?.RoleName ?? "Requestor";

                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, user.EmployeeName),
                    new(ClaimTypes.Role, roleName),
                    new("RoleId", user.RoleId.ToString()),
                    new("Section", user.Department?.DepartmentName?.ToString() ?? "unknown"),
                    new(ClaimTypes.NameIdentifier, user.Id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, "Login");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                var httpContext = httpContextAccessor.HttpContext;

                if (httpContext != null)
                {
                    await httpContext.SignInAsync("Login", claimsPrincipal, new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTime.UtcNow.AddHours(24)
                    });


                    if (password == "Password01")
                    {
                        return (true, true, "Please change your default password", null);
                    }


                    await dbcontext.SaveChangesAsync();
                }

                return (true, false, "Authentication Successfull.", roleName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Authentication failed due to an exception");
                return (false, false, $"SQL Error: {ex.Message}", null);
            }
        }

        public async Task<(bool isSuccess, string message)> Logout()
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return (false, "Context is null.");
            }

            await httpContext.SignOutAsync("Login");
            return (true, "Logout successfully..redirecting...");
        }

        public async Task<(bool isSuccess, string message)> ChangePassword(ChangePasswordDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return (false, "Password information is empty.");
                }

                if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                {
                    return (false, "Current password is required.");
                }

                if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                {
                    return (false, "New password must be at least 6 characters.");
                }

                if (dto.NewPassword != dto.ConfirmPassword)
                {
                    return (false, "New password and confirm password do not match.");
                }

                if (dto.CurrentPassword == dto.NewPassword)
                {
                    return (false, "New password cannot be the same as the current password.");
                }

                var httpContext = httpContextAccessor.HttpContext;
                var userIdStr = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return (false, "User is not authenticated or session has expired.");
                }

                var user = await dbcontext.UserTables.FindAsync(userId);
                if (user == null)
                {
                    return (false, "User account was not found.");
                }

                bool isCurrentValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password);
                if (!isCurrentValid)
                {
                    return (false, "Current password is incorrect.");
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

                // Save changes first to confirm DB persistence
                await dbcontext.SaveChangesAsync();

                // Sign out only after successful save
                if (httpContext != null)
                {
                    await httpContext.SignOutAsync("Login");
                }

                return (true, "Password changed successfully. Redirecting...");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while changing password.");
                return (false, "An unexpected error occurred while changing your password. Please try again.");
            }
        }
    }
}
