using Microsoft.EntityFrameworkCore;

namespace ShuttlOps.DBconnection
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
    }
}
