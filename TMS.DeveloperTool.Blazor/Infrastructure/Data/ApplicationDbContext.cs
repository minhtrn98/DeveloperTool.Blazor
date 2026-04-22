using Microsoft.EntityFrameworkCore;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<RouteCheckPoint> RouteCheckPoints { get; set; }
    public DbSet<RouteCheckPointTemplate> RouteCheckPointTemplates { get; set; }
    public DbSet<RequestHistory> RequestHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names to match PostgreSQL schema (lowercase with underscores)
        modelBuilder.Entity<Vehicle>()
            .ToTable("vehicles");

        modelBuilder.Entity<Employee>()
            .ToTable("employees");

        modelBuilder.Entity<RouteCheckPointTemplate>()
            .ToTable("route_checkpoint_templates");

        modelBuilder.Entity<RouteCheckPoint>()
            .ToTable("route_checkpoints");

        modelBuilder.Entity<RequestHistory>()
            .ToTable("request_histories");

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

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.BearerToken).HasColumnName("bearer_token");
            entity.Property(e => e.TokenExpiredAt).HasColumnName("token_expired_at");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Ignore(e => e.Email);
            entity.Ignore(e => e.Phone);
        });

        modelBuilder.Entity<RequestHistory>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Method).HasColumnName("method");
            entity.Property(e => e.Service).HasColumnName("service");
            entity.Property(e => e.Endpoint).HasColumnName("endpoint");
            entity.Property(e => e.JsonBody).HasColumnName("json_body");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => new { e.Name, e.Method, e.Service, e.Endpoint, e.JsonBody })
                .IsUnique();
        });
    }
}
