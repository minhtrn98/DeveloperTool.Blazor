using Dapper;
using Npgsql;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Data;

public sealed partial class ApplicationDbQuery(IConfiguration configuration, ILogger<ApplicationDbQuery> logger, string database)
{
    private readonly string? _connectionString = configuration.GetConnectionString(database);

    /// <summary>
    /// Whether a real connection string is configured for this database.
    /// Environments without access to the TMS production databases (e.g. only <c>LogApi</c>
    /// is available) simply omit the connection string instead of setting one — every query
    /// method below short-circuits with an empty/default result instead of throwing.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString) && _connectionString != "<<override>>";

    public async Task<T?> SingleOrDefaultAsync<T>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            LogNotConfigured(database);
            return default;
        }

        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        CommandDefinition commandDefinition = new(
            commandText: sql,
            parameters: parameters,
            cancellationToken: cancellationToken
        );
        LogSqlQuery(sql);
        return await connection.QuerySingleOrDefaultAsync<T>(commandDefinition);
    }

    public async Task<T?> FirstOrDefaultAsync<T>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            LogNotConfigured(database);
            return default;
        }

        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        CommandDefinition commandDefinition = new(
            commandText: sql,
            parameters: parameters,
            cancellationToken: cancellationToken
        );
        LogSqlQuery(sql);
        return await connection.QueryFirstOrDefaultAsync<T>(commandDefinition);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            LogNotConfigured(database);
            return [];
        }

        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        CommandDefinition commandDefinition = new(
            commandText: sql,
            parameters: parameters,
            cancellationToken: cancellationToken
        );
        LogSqlQuery(sql);
        return await connection.QueryAsync<T>(commandDefinition);
    }

    public async Task<IEnumerable<dynamic>> QueryAsync(string sql, object? parameters, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            LogNotConfigured(database);
            return [];
        }

        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        CommandDefinition commandDefinition = new(
            commandText: sql,
            parameters: parameters,
            cancellationToken: cancellationToken
        );
        LogSqlQuery(sql);
        return await connection.QueryAsync(commandDefinition);
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            LogNotConfigured(database);
            return 0;
        }

        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        CommandDefinition commandDefinition = new(
            commandText: sql,
            parameters: parameters,
            cancellationToken: cancellationToken
        );
        LogSqlQuery(sql);
        return await connection.ExecuteAsync(commandDefinition);
    }

    public async IAsyncEnumerable<T> QueryUnbufferedAsync<T>(string sql, object? parameters = null)
    {
        if (!IsConfigured)
        {
            LogNotConfigured(database);
            yield break;
        }

        await using NpgsqlConnection connection = new(_connectionString);
        await foreach (T item in connection.QueryUnbufferedAsync<T>(sql, parameters))
        {
            yield return item;
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "[DAPPER] Executing SQL query:\n{Sql}")]
    private partial void LogSqlQuery(string sql);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[DAPPER] Skipped query — no connection string configured for '{Database}'.")]
    private partial void LogNotConfigured(string database);
}
