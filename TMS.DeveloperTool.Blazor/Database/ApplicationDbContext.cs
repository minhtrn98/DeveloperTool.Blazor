using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Entities;

namespace TMS.DeveloperTool.Blazor.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<RouteCheckPoint> RouteCheckPoints { get; set; }
    public DbSet<RouteCheckPointTemplate> RouteCheckPointTemplates { get; set; }
}
