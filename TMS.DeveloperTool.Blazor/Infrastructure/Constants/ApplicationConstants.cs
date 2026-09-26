namespace TMS.DeveloperTool.Blazor.Infrastructure.Constants;

/// <summary>
/// Application-wide constants and magic strings.
/// </summary>
public static class ApplicationConstants
{
    /// <summary>
    /// Feature routes.
    /// </summary>
    public static class Routes
    {
        public const string Home = "/";
        public const string OrderStep1Trace = "/order-step1-trace";
        public const string OrderStep1Timeline = "/order-step1-timeline";
        public const string RouteStopTrace = "/route-stop-trace";
        public const string RouteStopQuery = "/route-stop-query";
        public const string PickupTaskTrace = "/pickup-task-trace";
        public const string PickupTaskTraceQuery = "/pickup-task-trace-query";
        public const string OrderStep1TracePro = "/pro/order-step1-trace";
        public const string OrderStep1TimelinePro = "/pro/order-step1-timeline";
        public const string OrderStep1QueryPro = "/pro/order-step1-query";
        public const string RouteStopQueryPro = "/pro/route-stop-query";
        public const string PickupTaskTraceQueryPro = "/pro/pickup-task-trace-query";
        public const string PickupTaskOrderTimelinePro = "/pro/pickup-task-order-timeline";
        public const string PickupTaskOrderItemTimelinePro = "/pro/pickup-task-order-item-timeline";
        public const string UnloadingHandoverPendingPro = "/pro/unloading-handover-pending";
        public const string DashboardPro = "/pro/dashboard";
    }

    public static class EnvironmentNames
    {
        public const string Local = "Local";
    }
}
