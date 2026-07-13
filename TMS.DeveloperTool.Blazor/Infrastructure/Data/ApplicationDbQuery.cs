using Dapper;
using Npgsql;
using System.Data;
using Microsoft.Extensions.Logging;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Data;

public sealed partial class ApplicationDbQuery(IConfiguration configuration, ILogger<ApplicationDbQuery> logger, string database)
{
    private readonly string? _connectionString = configuration.GetConnectionString(database);

    public async Task<T?> SingleOrDefaultAsync<T>(string sql, object? parameters, CancellationToken cancellationToken)
    {
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
        await using NpgsqlConnection connection = new(_connectionString);
        await foreach (T item in connection.QueryUnbufferedAsync<T>(sql, parameters))
        {
            yield return item;
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "[DAPPER] Executing SQL query:\n{Sql}")]
    private partial void LogSqlQuery(string sql);
}