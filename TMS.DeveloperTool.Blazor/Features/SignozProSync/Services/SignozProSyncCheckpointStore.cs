using System.Globalization;

namespace TMS.DeveloperTool.Blazor.Features.SignozProSync.Services;

/// <summary>
/// Persists the timestamp of the last successfully synced SigNoz Pro window to a plain text
/// file, so <see cref="SignozProSyncJob"/> resumes from where it left off across restarts
/// instead of re-querying from <see cref="SignozProOptions.QueryFromDate"/> every time.
/// </summary>
public sealed class SignozProSyncCheckpointStore(SignozProOptions signozProOptions, IWebHostEnvironment webHostEnvironment)
{
    private string ResolvedPath => Path.IsPathRooted(signozProOptions.CheckpointFilePath)
        ? signozProOptions.CheckpointFilePath
        : Path.Combine(webHostEnvironment.ContentRootPath, signozProOptions.CheckpointFilePath);

    public async Task<DateTimeOffset?> ReadAsync(CancellationToken cancellationToken)
    {
        string path = ResolvedPath;
        if (!File.Exists(path))
        {
            return null;
        }

        string text = (await File.ReadAllTextAsync(path, cancellationToken)).Trim();
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset value)
            ? value
            : null;
    }

    public async Task SaveAsync(DateTimeOffset checkpoint, CancellationToken cancellationToken)
    {
        string path = ResolvedPath;
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, checkpoint.ToString("O", CultureInfo.InvariantCulture), cancellationToken);
    }
}
