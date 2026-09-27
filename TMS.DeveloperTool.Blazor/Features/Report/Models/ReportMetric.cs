namespace TMS.DeveloperTool.Blazor.Features.Report.Models;

/// <summary>Values of the <c>metric</c> column in <c>report.daily_label_counts</c> / <c>report.daily_histograms</c>.</summary>
public static class ReportMetric
{
    // report.daily_label_counts
    public const string FailureType = "failure_type";
    public const string TransferSource = "transfer_source";
    public const string ArrivalGps = "arrival_gps";
    public const string ArrivalSuperseded = "arrival_superseded";

    // report.daily_histograms — "_m" values are meters, "_s" values are seconds.
    public const string ArrivalDistance = "arrival_distance_m";
    public const string HandoverReceive = "handover_receive_s";
    public const string HandoverConfirm = "handover_confirm_s";
    public const string HandoverFirstHandled = "handover_first_handled_s";
    public const string ManifestFirstArrival = "manifest_first_arrival_s";
    public const string ManifestLastComplete = "manifest_last_complete_s";
}
