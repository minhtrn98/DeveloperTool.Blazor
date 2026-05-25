using Dapper;
using Npgsql;
using System.Data;
using Microsoft.Extensions.Logging;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Data;

public sealed partial class ApplicationDbQuery(IConfiguration configuration, ILogger<ApplicationDbQuery> logger, string database)
{
    private readonly string? _connectionString = configuration.GetConnectionString(database);
    private NpgsqlConnection? _connection;

    public async Task<T?> SingleOrDefaultAsync<T>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        _connection ??= new NpgsqlConnection(_connectionString);
        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }
        CommandDefinition commandDefinition = new(
            commandText: sql,
            parameters: parameters,
            cancellationToken: cancellationToken
        );
        LogSqlQuery(sql);
        return await _connection.QuerySingleOrDefaultAsync<T>(commandDefinition);
    }

    public async Task<T?> FirstOrDefaultAsync<T>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        _connection ??= new NpgsqlConnection(_connectionString);
        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }
        CommandDefinition commandDefinition = new(
            commandText: sql,
            parameters: parameters,
            cancellationToken: cancellationToken
        );
        LogSqlQuery(sql);
        return await _connection.QueryFirstOrDefaultAsync<T>(commandDefinition);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        _connection ??= new NpgsqlConnection(_connectionString);
        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

        CommandDefinition commandDefinition = new(
            commandText: sql,
            parameters: parameters,
            cancellationToken: cancellationToken
        );
        LogSqlQuery(sql);
        return await _connection.QueryAsync<T>(commandDefinition);
    }

    public async Task<IEnumerable<dynamic>> QueryAsync(string sql, object? parameters, CancellationToken cancellationToken)
    {
        _connection ??= new NpgsqlConnection(_connectionString);
        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

        CommandDefinition commandDefinition = new(
            commandText: sql,
            parameters: parameters,
            cancellationToken: cancellationToken
        );
        LogSqlQuery(sql);
        return await _connection.QueryAsync(commandDefinition);
    }

    public IAsyncEnumerable<T> QueryUnbufferedAsync<T>(string sql, object? parameters = null)
    {
        _connection ??= new NpgsqlConnection(_connectionString);
        return _connection.QueryUnbufferedAsync<T>(sql, parameters);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[DAPPER] Executing SQL query:\n{Sql}")]
    private partial void LogSqlQuery(string sql);
}