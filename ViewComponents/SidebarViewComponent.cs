using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShuttlOps.Models;

namespace ShuttlOps.ViewComponents
{
    public class SidebarViewComponent(ShuttlOpsDbContext shuttlOpsDbContext) : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var roleClaim = HttpContext.User.FindFirst("RoleId")?.Value;
            if(string.IsNullOrEmpty(roleClaim) || !int.TryParse(roleClaim, out var roleId))
            {
                return View(new HashSet<string>());
            }

            var permittedModules = await shuttlOpsDbContext.PermissionsTables
                                  .Include(rp => rp.Module)
                                  .Where(rp => rp.RoleId == roleId
                                         && rp.CanView == true
                                         && rp.Module.IsActive == true)
                                  .Select(rp => rp.Module.ModuleName)
                                  .ToListAsync();
            return View(new HashSet<string>(permittedModules)); 
        }
    }
}