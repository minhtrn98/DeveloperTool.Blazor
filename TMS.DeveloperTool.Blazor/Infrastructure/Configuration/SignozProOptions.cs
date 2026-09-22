using System.ComponentModel.DataAnnotations;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

public sealed class SignozProOptions
{
    public const string SectionName = "SignozPro";

    /// <summary>
    /// Name of the named HTTP client registered for SigNoz Pro calls, configured with a short
    /// timeout (see <c>ExternalApiServiceExtensions.AddExternalApis</c>) so a stuck request
    /// fails fast and can be retried instead of blocking for minutes.
    /// </summary>
    public const string HttpClientName = "SignozPro";

    [Required]
    public required string BaseUrl { get; init; }

    /// <summary>
    /// Seed access token used only for the very first rotate call's Authorization header
    /// (the rotate endpoint expects a Bearer token alongside the refresh token). Once a
    /// rotate succeeds, the rotated access token is persisted in the "pro" schema and this
    /// value is no longer used.
    /// </summary>
    public string? InitialAccessToken { get; init; }

    /// <summary>
    /// Seed refresh token used to bootstrap SigNoz Pro access the very first time.
    /// Once a rotate succeeds, the rotated refresh token is persisted in the "pro" schema and
    /// this value is no longer used.
    /// </summary>
    public string? InitialRefreshToken { get; init; }

    /// <summary>
    /// Earliest point in time the sync job is allowed to query — e.g. set to 2026-01-01 to
    /// only ever pull data from that date onward. Only used when no checkpoint file exists yet
    /// (the very first run, or after the checkpoint file was deleted); every run after that is
    /// bounded by the saved checkpoint instead. Leave unset to fall back to a short lookback
    /// window on the first run.
    /// </summary>
    public DateTimeOffset? QueryFromDate { get; init; }

    /// <summary>
    /// Path to the text file that stores the timestamp of the last successfully synced window,
    /// so the job resumes from where it left off instead of re-querying from
    /// <see cref="QueryFromDate"/> every run. Relative paths resolve against the app's content
    /// root.
    /// </summary>
    public string CheckpointFilePath { get; init; } = "App_Data/signoz-pro-sync-checkpoint.txt";

    /// <summary>
    /// Lower bound (inclusive) of the randomized delay between consecutive paginated requests
    /// to SigNoz Pro within the same query — a random delay in [<see cref="RequestDelayMinMs"/>,
    /// <see cref="RequestDelayMaxMs"/>) is picked before every request after the first, so a
    /// large backfill doesn't fire a fixed, easily-fingerprinted request cadence. Milliseconds.
    /// </summary>
    public int RequestDelayMinMs { get; init; } = 300;

    /// <summary>
    /// Upper bound (exclusive) of the randomized delay between consecutive paginated requests
    /// to SigNoz Pro. See <see cref="RequestDelayMinMs"/>. Milliseconds.
    /// </summary>
    public int RequestDelayMaxMs { get; init; } = 1200;
}
