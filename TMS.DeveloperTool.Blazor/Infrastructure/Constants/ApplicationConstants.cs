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
        public const string Pairing = "/pairing";
        public const string PickupTasks = "/pickup-tasks";
        public const string DriverChange = "/driver-change";
        public const string JsonBuilder = "/json-builder";
        public const string JobTrigger = "/job-trigger";
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
