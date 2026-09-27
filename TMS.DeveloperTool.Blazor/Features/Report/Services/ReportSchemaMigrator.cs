using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Npgsql;

namespace TMS.DeveloperTool.Blazor.Features.Report.Services;

/// <summary>
/// Applies the embedded <c>Features/Report/Migrations/V{version}__{description}.sql</c> scripts
/// in version order and records each one in <c>report.schema_versions</c>. To change the report
/// schema, add the next <c>V00N__*.sql</c> file — never edit one that has already been applied
/// (a changed checksum is logged as a warning, not re-run).
/// </summary>
public sealed partial class ReportSchemaMigrator(ConnectionStringsOptions connectionStrings, ILogger<ReportSchemaMigrator> logger)
{
    private const string ResourceMarker = ".Features.Report.Migrations.";

    // Session-level advisory lock so two app instances starting together don't both migrate.
    private const long MigrationLockKey = 7_261_001;

    private const string BootstrapSql = """
        CREATE SCHEMA IF NOT EXISTS report;
        CREATE TABLE IF NOT EXISTS report.schema_versions (
            version INT PRIMARY KEY,
            description TEXT NOT NULL,
            script_name TEXT NOT NULL,
            checksum TEXT NOT NULL,
            applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """;

    public sealed record MigrationScript(int Version, string Description, string ScriptName, string Sql)
    {
        public string Checksum { get; } = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Sql)));
    }

    private readonly Lock ensureGate = new();
    private Task? ensureTask;

    /// <summary>
    /// Migrates once per app lifetime (retried after a failure). For readers such as the dashboard,
    /// which need the report tables even when <c>BackgroundJobs:Enabled</c> is false.
    /// </summary>
    public Task EnsureMigratedAsync()
    {
        lock (ensureGate)
        {
            if (ensureTask is null || ensureTask.IsFaulted || ensureTask.IsCanceled)
            {
                ensureTask = MigrateAsync(CancellationToken.None);
            }

            return ensureTask;
        }
    }

    /// <summary>Applies pending scripts; returns the versions applied by this call.</summary>
    public async Task<List<int>> MigrateAsync(CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = new(connectionStrings.DeveloperDb);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition("SELECT pg_advisory_lock(@Key)", new { Key = MigrationLockKey }, cancellationToken: cancellationToken));

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(BootstrapSql, cancellationToken: cancellationToken));

            Dictionary<int, string> applied = (await connection.QueryAsync<(int Version, string Checksum)>(
                    new CommandDefinition("SELECT version, checksum FROM report.schema_versions", cancellationToken: cancellationToken)))
                .ToDictionary(x => x.Version, x => x.Checksum);

            List<int> appliedNow = [];
            foreach (MigrationScript script in LoadScripts())
            {
                if (applied.TryGetValue(script.Version, out string? checksum))
                {
                    if (!string.Equals(checksum, script.Checksum, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogWarning("Report migration {Script} changed after it was applied; add a new version instead of editing it.", script.ScriptName);
                    }

                    continue;
                }

                await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
                await connection.ExecuteAsync(new CommandDefinition(script.Sql, transaction: transaction, commandTimeout: 600, cancellationToken: cancellationToken));
                await connection.ExecuteAsync(new CommandDefinition(
                    "INSERT INTO report.schema_versions (version, description, script_name, checksum) VALUES (@Version, @Description, @ScriptName, @Checksum)",
                    new { script.Version, script.Description, script.ScriptName, script.Checksum },
                    transaction,
                    cancellationToken: cancellationToken));
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation("Applied report migration {Script}.", script.ScriptName);
                appliedNow.Add(script.Version);
            }

            return appliedNow;
        }
        finally
        {
            await connection.ExecuteAsync(new CommandDefinition("SELECT pg_advisory_unlock(@Key)", new { Key = MigrationLockKey }, cancellationToken: CancellationToken.None));
        }
    }

    public static List<MigrationScript> LoadScripts()
    {
        Assembly assembly = typeof(ReportSchemaMigrator).Assembly;
        List<MigrationScript> scripts = [];

        foreach (string resourceName in assembly.GetManifestResourceNames().Where(x => x.Contains(ResourceMarker, StringComparison.Ordinal)))
        {
            string scriptName = resourceName[(resourceName.IndexOf(ResourceMarker, StringComparison.Ordinal) + ResourceMarker.Length)..];
            Match match = ScriptNamePattern().Match(scriptName);
            if (!match.Success)
            {
                throw new InvalidOperationException($"Report migration '{scriptName}' must be named V<version>__<description>.sql.");
            }

            using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
            using StreamReader reader = new(stream);
            scripts.Add(new MigrationScript(
                int.Parse(match.Groups["version"].Value),
                match.Groups["description"].Value.Replace('_', ' '),
                scriptName,
                reader.ReadToEnd()));
        }

        List<int> duplicates = scripts.GroupBy(x => x.Version).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException($"Duplicate report migration versions: {string.Join(", ", duplicates)}.");
        }

        return scripts.OrderBy(x => x.Version).ToList();
    }

    [GeneratedRegex(@"^V(?<version>\d+)__(?<description>.+)\.sql$", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptNamePattern();
}
