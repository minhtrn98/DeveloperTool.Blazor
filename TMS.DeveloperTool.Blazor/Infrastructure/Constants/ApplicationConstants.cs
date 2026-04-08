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
        public const string Pairing = "/pairing";
        public const string PairingConfirmSwap = "/pairing/confirm-swap";
        public const string SimulateRoute = "/simulate-route";
        public const string PickupTasks = "/pickup-tasks";
        public const string PickupTasksCreateEvent = "/pickup-tasks/create-event";
        public const string TopMovingVehicles = "/top-moving-vehicles";
        public const string VehicleStatuses = "/vehicle-statuses";
        public const string RoutingConfig = "/routing-config";
        public const string DriverChange = "/driver-change";
        public const string JsonBuilder = "/json-builder";
        public const string JobTrigger = "/job-trigger";
        public const string ApiRequest = "/api-request";
    }

    public static class EnvironmentNames
    {
        public const string Local = "Local";
    }

    public static class HttpHeaders
    {
        public const string XForwardedFor = "X-Forwarded-For";
    }

    public static class MenuKeys
    {
        public const string Home = "Home";
        public const string Pairing = "Pairing";
        public const string ConfirmSwap = "ConfirmSwap";
        public const string SimulateRoute = "SimulateRoute";
        public const string PickupTasks = "PickupTasks";
        public const string CreatePickupTaskEvent = "CreatePickupTaskEvent";
        public const string TopMovingVehicles = "TopMovingVehicles";
        public const string VehicleStatuses = "VehicleStatuses";
        public const string RoutingConfig = "RoutingConfig";
        public const string ApiRequest = "ApiRequest";
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
