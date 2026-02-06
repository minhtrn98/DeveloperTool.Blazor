using Microsoft.EntityFrameworkCore;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<RouteCheckPoint> RouteCheckPoints { get; set; }
    public DbSet<RouteCheckPointTemplate> RouteCheckPointTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names to match PostgreSQL schema (lowercase with underscores)
        modelBuilder.Entity<Vehicle>()
            .ToTable("vehicles");

        modelBuilder.Entity<RouteCheckPointTemplate>()
            .ToTable("route_checkpoint_templates");

        modelBuilder.Entity<RouteCheckPoint>()
            .ToTable("route_checkpoints");

        // Configure column names to match PostgreSQL schema
        modelBuilder.Entity<RouteCheckPointTemplate>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.JumpSeconds).HasColumnName("jump_seconds");
        });

        modelBuilder.Entity<RouteCheckPoint>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Lon).HasColumnName("lon");
            entity.Property(e => e.Lat).HasColumnName("lat");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Km).HasColumnName("km");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.TemplateId).HasColumnName("template_id");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.Property(e => e.LicensePlate).HasColumnName("license_plate");
            entity.Property(e => e.LastOdo).HasColumnName("last_odo");
            entity.Property(e => e.IsMoving).HasColumnName("is_moving");
        });
    }
}
