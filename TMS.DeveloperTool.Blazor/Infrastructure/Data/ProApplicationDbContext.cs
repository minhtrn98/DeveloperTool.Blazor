using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Data;

public class ProApplicationDbContext(DbContextOptions<ProApplicationDbContext> options) : DbContext(options)
{
    public DbSet<OrderStep1TraceLog> OrderStep1TraceLogs { get; set; }
    public DbSet<RouteStopTraceLog> RouteStopTraceLogs { get; set; }
    public DbSet<PickupTaskTraceLog> PickupTaskTraceLogs { get; set; }
    public DbSet<LogApiToken> LogApiTokens { get; set; }
    public DbSet<PickupTaskOrder> PickupTaskOrders { get; set; }
    public DbSet<PickupTaskOrderItem> PickupTaskOrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("pro");

        // Configure table names to match PostgreSQL schema (lowercase with underscores)
        modelBuilder.Entity<OrderStep1TraceLog>()
            .ToTable("order_step1_trace_logs");

        modelBuilder.Entity<RouteStopTraceLog>()
            .ToTable("route_stop_trace_logs");

        modelBuilder.Entity<PickupTaskTraceLog>()
            .ToTable("pickup_task_trace_logs");

        modelBuilder.Entity<LogApiToken>()
            .ToTable("log_api_tokens");

        modelBuilder.Entity<PickupTaskOrder>()
            .ToTable("pickup_task_orders");

        modelBuilder.Entity<PickupTaskOrderItem>()
            .ToTable("pickup_task_order_items");

        // Configure column names to match PostgreSQL schema
        modelBuilder.Entity<OrderStep1TraceLog>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.TraceId).HasColumnName("trace_id");
            entity.Property(e => e.SpanId).HasColumnName("span_id");
            entity.Property(e => e.LogTimestamp).HasColumnName("log_timestamp");
            entity.Property(e => e.MessageDetail).HasColumnName("message_detail");
            entity.Property(e => e.Env).HasColumnName("env");
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
            entity.Property(e => e.ActionObjectId).HasColumnName("action_object_id");
            entity.Property(e => e.MessageDetail).HasColumnName("message_detail");
            entity.Property(e => e.Env).HasColumnName("env");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.LogId).IsUnique();
        });

        modelBuilder.Entity<PickupTaskTraceLog>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.TraceId).HasColumnName("trace_id");
            entity.Property(e => e.SpanId).HasColumnName("span_id");
            entity.Property(e => e.LogTimestamp).HasColumnName("log_timestamp");
            entity.Property(e => e.PickupTaskId).HasColumnName("pickup_task_id");
            entity.Property(e => e.DeliveryLineId).HasColumnName("delivery_line_id");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.MessageDetail).HasColumnName("message_detail");
            entity.Property(e => e.Env).HasColumnName("env");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.IsProcessed).HasColumnName("is_processed");
            entity.HasIndex(e => e.LogId).IsUnique();
        });

        modelBuilder.Entity<LogApiToken>(entity =>
        {
            entity.HasKey(e => e.Env);
            entity.Property(e => e.Env).HasColumnName("env");
            entity.Property(e => e.AccessToken).HasColumnName("access_token");
            entity.Property(e => e.AccessTokenExpiredAt).HasColumnName("access_token_expired_at");
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token");
            entity.Property(e => e.RefreshTokenExpiredAt).HasColumnName("refresh_token_expired_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<PickupTaskOrder>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TraceId).HasColumnName("trace_id").HasMaxLength(64);
            entity.Property(e => e.PickupTaskId).HasColumnName("pickup_task_id").HasMaxLength(20);
            entity.Property(e => e.OrderId).HasColumnName("order_id").HasMaxLength(20);
            entity.Property(e => e.ExtraServices).HasColumnName("extra_services").HasMaxLength(20);
            entity.Property(e => e.Weight).HasColumnName("weight");
            entity.Property(e => e.RealWeight).HasColumnName("real_weight");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion(status => status.Description, description => OrderStatus.FromDescription(description)).HasMaxLength(50);
            entity.Property(e => e.IsProcessCompleted).HasColumnName("is_process_completed");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => new { e.PickupTaskId, e.OrderId });
        });

        modelBuilder.Entity<PickupTaskOrderItem>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TraceId).HasColumnName("trace_id").HasMaxLength(64);
            entity.Property(e => e.PickupTaskId).HasColumnName("pickup_task_id").HasMaxLength(20);
            entity.Property(e => e.OrderId).HasColumnName("order_id").HasMaxLength(20);
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id").HasMaxLength(20);
            entity.Property(e => e.Weight).HasColumnName("weight");
            entity.Property(e => e.RealWeight).HasColumnName("real_weight");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion(status => status.Description, description => OrderStatus.FromDescription(description)).HasMaxLength(50);
            entity.Property(e => e.IsProcessCompleted).HasColumnName("is_process_completed");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => new { e.PickupTaskId, e.OrderItemId });
        });
    }
}
