using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using ShuttlOps.Models;
using System.Security.Claims;

namespace ShuttlOps.Services
{
    public class Authenticationservice(
        ShuttlOpsDbContext dbcontext,
        ILogger<AuthenticationService> logger,
        IHttpContextAccessor httpContextAccessor) : IAuthenticationservice
    {

        public async Task<(bool isSuccess, string message)> AuthenticateUser(string username, string password)
        {
            try
            {
                var user = await dbcontext.UserTables
                                .Where(x => x.UserName == username)
                                .Include(m => m.Role)
                                .FirstOrDefaultAsync();
                if (user == null)
                {
                    return (false, "Invalid username or password.");
                }

                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
                if (!isPasswordValid)
                {
                    return (false, "Incorrect password.");
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, user.EmployeeName),
                    new(ClaimTypes.Role, user.Role.RoleName),
                    new("RoleId", user.RoleId.ToString()),
                    new("Section", user.Department?.DepartmentName?.ToString() ?? "unknown"),
                    new(ClaimTypes.NameIdentifier, user.Id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, "Login");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                var httpContext = httpContextAccessor.HttpContext;

                if(httpContext != null)
                {
                    await httpContext.SignInAsync("Login", claimsPrincipal, new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTime.UtcNow.AddHours(24)
                    });

                    await dbcontext.SaveChangesAsync();
                }

                return (true, "Authentication Successfull.");
            }catch(Exception ex)
            {
                return(false, $"SQL Error: {ex.Message}");
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
    }
}
