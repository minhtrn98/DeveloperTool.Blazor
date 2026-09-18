namespace TMS.DeveloperTool.Blazor.Infrastructure.Constants;

/// <summary>
/// Application-wide constants and magic strings.
/// </summary>
public static class ApplicationConstants
{
    /// <summary>
    /// TMS Database query service keys.
    /// </summary>
    public static class DatabaseContextKeys
    {
        public const string DriverDb = "DriverDb";
        public const string FleetDb = "FleetDb";
        public const string RouteDb = "RouteDb";
        public const string PlanningDb = "PlanningDb";
        public const string OrderDb = "OrderDb";
    }

    /// <summary>
    /// API-related constants.
    /// </summary>
    public static class ApiDefaults
    {
        public const int RetryCount = 3;
        public const int RetryDelayMs = 5000;
    }

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
    }

    public static class EnvironmentNames
    {
        public const string Local = "Local";
    }

    /// <summary>
    /// Error and status pages.
    /// </summary>
    public static class ErrorRoutes
    {
        public const string ErrorPage = "/Error";
        public const string NotFound = "/not-found";
    }
}
