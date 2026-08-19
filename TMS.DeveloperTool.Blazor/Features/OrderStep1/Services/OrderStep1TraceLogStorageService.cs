using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Models;

namespace TMS.DeveloperTool.Blazor.Features.OrderStep1.Services;

public sealed class OrderStep1TraceLogStorageService(ApplicationDbContext dbContext)
{
    public async Task<bool> SaveIfNewAsync(OrderStep1TraceLogEntry entry, CancellationToken cancellationToken)
    {
        int rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO order_step1_trace_logs (log_id, order_id, trace_id, span_id, log_timestamp, message_detail, created_at)
            VALUES ({entry.LogId}, {entry.OrderId}, {entry.TraceId}, {entry.SpanId}, {entry.Timestamp.ToUniversalTime()}, {entry.MessageDetail}, {DateTimeOffset.UtcNow})
            ON CONFLICT (log_id) DO NOTHING;
            """, cancellationToken);

        return rowsAffected > 0;
    }
}
