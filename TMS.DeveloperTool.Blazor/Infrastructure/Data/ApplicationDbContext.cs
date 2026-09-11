using Microsoft.EntityFrameworkCore;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<RouteCheckPoint> RouteCheckPoints { get; set; }
    public DbSet<RouteCheckPointTemplate> RouteCheckPointTemplates { get; set; }
    public DbSet<RequestHistory> RequestHistories { get; set; }
    public DbSet<OrderStep1TraceLog> OrderStep1TraceLogs { get; set; }
    public DbSet<RouteStopTraceLog> RouteStopTraceLogs { get; set; }
    public DbSet<LogApiToken> LogApiTokens { get; set; }

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

        modelBuilder.Entity<OrderStep1TraceLog>()
            .ToTable("order_step1_trace_logs");

        modelBuilder.Entity<RouteStopTraceLog>()
            .ToTable("route_stop_trace_logs");

        modelBuilder.Entity<LogApiToken>()
            .ToTable("log_api_tokens");

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

        modelBuilder.Entity<OrderStep1TraceLog>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.TraceId).HasColumnName("trace_id");
            entity.Property(e => e.SpanId).HasColumnName("span_id");
            entity.Property(e => e.LogTimestamp).HasColumnName("log_timestamp");
            entity.Property(e => e.MessageDetail).HasColumnName("message_detail");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.LogId).IsUnique();
        });

        modelBuilder.Entity<RouteStopTraceLog>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.TraceId).HasColumnName("trace_id");
            entity.Property(e => e.SpanId).HasColumnName("span_id");
            entity.Property(e => e.LogTimestamp).HasColumnName("log_timestamp");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.Driver).HasColumnName("driver");
            entity.Property(e => e.Office).HasColumnName("office");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.VehicleId).HasColumnName("vehicle_id");
            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.MessageDetail).HasColumnName("message_detail");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.LogId).IsUnique();
        });

        modelBuilder.Entity<LogApiToken>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccessToken).HasColumnName("access_token");
            entity.Property(e => e.AccessTokenExpiredAt).HasColumnName("access_token_expired_at");
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token");
            entity.Property(e => e.RefreshTokenExpiredAt).HasColumnName("refresh_token_expired_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });
    }
}
